using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Obganism;

namespace Nap.Obganism {
	public static class ObganismNapConverter {
		/// <summary>
		/// Convert a list of Obganism object into a Nap container.
		/// If the provided <paramref name="obganismSource"/> starts with a modifier list
		/// not attached to an object (as per the example below):
		///
		/// <list type="bullet">
		/// <item> The optional <c>name</c> modifier will define the name of the container. </item>
		/// </list>
		///
		/// Example using loose/detached modifiers:
		/// <code>
		/// -- (
		///    name(the container name)
		/// )
		///
		/// cat {
		///    name : string -- in(1, 30)
		///    is grumpy : bool -- default(true)
		/// }
		/// </code>
		/// </summary>
		///
		/// <exception cref="ObganismException">
		/// Thrown when <paramref name="obganismSource"/> isn't valid Obganism.
		/// </exception>
		public static PartialContainer ObjectsToContainer(string obganismSource) {
			if (obganismSource.TrimStart().StartsWith("--")) {
				var obganismObjects = ObganismSerializer.Deserialize("c{}" + obganismSource);

				obganismObjects[0].Type.Name = string.Empty;

				return ObjectsToContainer(obganismObjects);
			}

			else return ObjectsToContainer(ObganismSerializer.Deserialize(obganismSource));
		}

		/// <summary>
		/// Convert a list of Obganism object into a Nap container.
		/// If the first object type is blank-named, its modifiers will be used to configure the container itself:
		///
		/// <list type="bullet">
		/// <item> The optional <c>name</c> modifier will define the name of the container. </item>
		/// </list>
		/// </summary>
		public static PartialContainer ObjectsToContainer(IReadOnlyList<ObganismObject> obganismObjects) {
			var container = new PartialContainer();

			var i = 0;

			// The first object is unnamed: use it as the container's configuration.
			if (obganismObjects.Count != 0 && string.IsNullOrWhiteSpace(obganismObjects[0].Type.Name)) {
				i = 1;

				foreach (var obganismModifier in obganismObjects[0].Modifiers)
					if (obganismModifier.Name == "name" && GetModifierParameterStringValue(obganismModifier, 0) is string containerName) {
						container.Name = containerName;
						break;
					}
			}

			for (; i < obganismObjects.Count; ++i)
				container.Resources.Add(ObjectToResource(obganismObjects[i]));

			return container;
		}

		/// <exception cref="ObganismException">
		/// Thrown when <paramref name="obganismSource"/> isn't valid Obganism.
		/// </exception>
		public static IReadOnlyCollection<PartialResource> ObjectsToResources(string obganismSource) =>
			ObjectsToResources(ObganismSerializer.Deserialize(obganismSource));

		public static IReadOnlyCollection<PartialResource> ObjectsToResources(IReadOnlyList<ObganismObject> obganismObjects) {
			var resources = new PartialResource[obganismObjects.Count];

			for (var i = 0; i < resources.Length; ++i)
				resources[i] = ObjectToResource(obganismObjects[i]);

			return resources;
		}

		public static PartialResource ObjectToResource(ObganismObject obganismObject) {
			var resource = new PartialResource { Name = obganismObject.Type.Name };

			var i = 0;
			foreach (var obganismGeneric in obganismObject.Type.Generics)
				resource.GenericTemplates[i++] = obganismGeneric.Name;

			foreach (var obganismProperty in obganismObject.Properties)
				resource.Fields[obganismProperty.Name] = PropertyToFieldType(obganismProperty);

			return resource;
		}

		// ----

		static PartialFieldType PropertyToFieldType(ObganismProperty obganismProperty) {
			var fieldType = TypeToFieldType(obganismProperty.Type);

			foreach (var obganismModifier in obganismProperty.Modifiers)
				ModifierToMeta(fieldType, obganismModifier);

			return fieldType;
		}

		static PartialFieldType TypeToFieldType(ObganismType obganismType) {
			var fieldType = new PartialFieldType { Name = obganismType.Name };

			var i = 0;
			foreach (var obganismGeneric in obganismType.Generics)
				fieldType.Generics.Add(i++, TypeToFieldType(obganismGeneric));

			return fieldType;
		}

		// Modifiers to Meta
		// ----

		static void ModifierToMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			switch (obganismModifier.Name) {
				// Built-in Meta
				// ----

				case "min":
				case "at least":
				case "not below":
				case "not before":
					ModifierToMinMeta(fieldType, obganismModifier, true); break;
				case "above":
				case "after":
					ModifierToMinMeta(fieldType, obganismModifier, false); break;

				case "max":
				case "at most":
				case "not after":
				case "not above":
					ModifierToMaxMeta(fieldType, obganismModifier, true); break;
				case "below":
				case "before":
					ModifierToMaxMeta(fieldType, obganismModifier, false); break;

				case "between": ModifierToMinMaxMeta(fieldType, obganismModifier, false); break;
				case "in": ModifierToMinMaxMeta(fieldType, obganismModifier, true); break;

				case "enum":
				case "one of":
				case "amongst":
					ModifierToAllowedValuesMeta(fieldType, obganismModifier); break;

				case "not one of":
				case "not amongst":
					ModifierToForbiddenValuesMeta(fieldType, obganismModifier); break;

				case "allow empty": fieldType.Meta[NapBuiltInMeta.AllowEmpty] = true; break;
				case "not empty": fieldType.Meta[NapBuiltInMeta.AllowEmpty] = false; break;

				case "multiline": fieldType.Meta[NapBuiltInMeta.AllowMultiline] = true; break;
				case "not multiline": fieldType.Meta[NapBuiltInMeta.AllowMultiline] = false; break;

				case "pattern": ModifierToPatternMeta(fieldType, obganismModifier); break;

				case "same as": ModifierToSameAsMeta(fieldType, obganismModifier); break;
				case "not same as": ModifierToNotSameAsMeta(fieldType, obganismModifier); break;

				case "self": fieldType.Meta[NapBuiltInMeta.SelfReference] = SelfReference.Enforce; break;
				case "not self": fieldType.Meta[NapBuiltInMeta.SelfReference] = SelfReference.Forbid; break;

				case "optional": fieldType.Meta[NapBuiltInMeta.IsOptional] = true; break;
				case "not optional": fieldType.Meta[NapBuiltInMeta.IsOptional] = false; break;

				case "default": ModifierToDefaultMeta(fieldType, obganismModifier); break;

				case "allow duplicates": fieldType.Meta[NapBuiltInMeta.AllowDuplicates] = true; break;
				case "no duplicates": fieldType.Meta[NapBuiltInMeta.AllowDuplicates] = false; break;

				// Collection Generic Meta
				// ----

				case "all min":
				case "all at least":
				case "not any below":
				case "not any before":
					ModifierToMinMeta(GetMetaTargetGeneric(fieldType), obganismModifier, true); break;
				case "all above":
				case "all after":
					ModifierToMinMeta(GetMetaTargetGeneric(fieldType), obganismModifier, false); break;

				case "all max":
				case "all at most":
				case "not any after":
				case "not any above":
					ModifierToMaxMeta(GetMetaTargetGeneric(fieldType), obganismModifier, true); break;
				case "all below":
				case "all before":
					ModifierToMaxMeta(GetMetaTargetGeneric(fieldType), obganismModifier, false); break;

				case "all between": ModifierToMinMaxMeta(GetMetaTargetGeneric(fieldType), obganismModifier, false); break;
				case "all in": ModifierToMinMaxMeta(GetMetaTargetGeneric(fieldType), obganismModifier, true); break;

				case "all one of":
				case "all amongst":
					ModifierToAllowedValuesMeta(GetMetaTargetGeneric(fieldType), obganismModifier); break;

				case "not any one of":
				case "not any amongst":
					ModifierToForbiddenValuesMeta(GetMetaTargetGeneric(fieldType), obganismModifier); break;

				case "any empty": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.AllowEmpty] = true; break;
				case "not any empty": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.AllowEmpty] = false; break;

				case "all multiline": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.AllowMultiline] = true; break;
				case "not any multiline": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.AllowMultiline] = false; break;

				case "all pattern": ModifierToPatternMeta(GetMetaTargetGeneric(fieldType), obganismModifier); break;

				case "all same as": ModifierToSameAsMeta(GetMetaTargetGeneric(fieldType), obganismModifier); break;
				case "not any same as": ModifierToNotSameAsMeta(GetMetaTargetGeneric(fieldType), obganismModifier); break;

				case "all self": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.SelfReference] = SelfReference.Enforce; break;
				case "not any self": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.SelfReference] = SelfReference.Forbid; break;

				case "all optional": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.IsOptional] = true; break;
				case "not any optional": GetMetaTargetGeneric(fieldType).Meta[NapBuiltInMeta.IsOptional] = false; break;

				// Custom Meta
				// ----

				//default: ModifierToCustomMeta(fieldType, obganismModifier); break;
			}
		}

		static PartialFieldType GetMetaTargetGeneric(PartialFieldType fieldType) {
			var targetGenericIndex = fieldType.Name == NapBuiltInTypes.Map ? 1 : 0;

			if (!fieldType.Generics.TryGetValue(targetGenericIndex, out var targetGenericType))
				fieldType.Generics[targetGenericIndex] = targetGenericType = new PartialFieldType();

			return targetGenericType;
		}

		static void ModifierToMinMeta(PartialFieldType fieldType, ObganismModifier obganismModifier, bool defaultInclusivity) {
			ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 0, NapBuiltInMeta.Min);
			ModifierParameterToIsInclusiveMeta(fieldType, obganismModifier, 1, NapBuiltInMeta.MinIsInclusive, defaultInclusivity);
		}

		static void ModifierToMaxMeta(PartialFieldType fieldType, ObganismModifier obganismModifier, bool defaultInclusivity) {
			ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 0, NapBuiltInMeta.Max);
			ModifierParameterToIsInclusiveMeta(fieldType, obganismModifier, 1, NapBuiltInMeta.MaxIsInclusive, defaultInclusivity);
		}

		static void ModifierToMinMaxMeta(PartialFieldType fieldType, ObganismModifier obganismModifier, bool defaultInclusivity) {
			// range(min, max)
			if (obganismModifier.Parameters.Count == 2) {
				ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 0, NapBuiltInMeta.Min);
				ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 1, NapBuiltInMeta.Max);

				fieldType.Meta[NapBuiltInMeta.MinIsInclusive] =
				fieldType.Meta[NapBuiltInMeta.MaxIsInclusive] = defaultInclusivity;
			}

			else if (obganismModifier.Parameters.Count == 3) {
				ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 0, NapBuiltInMeta.Min);

				// range(min, min_inclusive, max)
				if (GetModifierParameterValue(obganismModifier, 1) is string minInclusivity) {
					fieldType.Meta[NapBuiltInMeta.MinIsInclusive] = minInclusivity.StartsWith("in", StringComparison.InvariantCultureIgnoreCase);

					ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 2, NapBuiltInMeta.Max);

					fieldType.Meta[NapBuiltInMeta.MaxIsInclusive] = defaultInclusivity;
				}

				// range(min, max, max_inclusive)
				else {
					fieldType.Meta[NapBuiltInMeta.MinIsInclusive] = defaultInclusivity;

					ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 1, NapBuiltInMeta.Max);
					ModifierParameterToIsInclusiveMeta(fieldType, obganismModifier, 2, NapBuiltInMeta.MaxIsInclusive, defaultInclusivity);
				}
			}

			// range(min, min_inclusive, max, max_inclusive)
			else if (4 <= obganismModifier.Parameters.Count) {
				ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 0, NapBuiltInMeta.Min);
				ModifierParameterToIsInclusiveMeta(fieldType, obganismModifier, 1, NapBuiltInMeta.MinIsInclusive, defaultInclusivity);
				ModifierParameterToMinMaxMeta(fieldType, obganismModifier, 2, NapBuiltInMeta.Max);
				ModifierParameterToIsInclusiveMeta(fieldType, obganismModifier, 3, NapBuiltInMeta.MaxIsInclusive, defaultInclusivity);
			}
		}

		static void ModifierParameterToMinMaxMeta(PartialFieldType fieldType, ObganismModifier obganismModifier, int parameterIndex, string metaName) {
			fieldType.Meta[metaName] = fieldType.Name switch {
				NapBuiltInTypes.Float => GetModifierParameterFloatValue(obganismModifier, parameterIndex),
				NapBuiltInTypes.Date => GetModifierParameterDateValue(obganismModifier, parameterIndex),
				NapBuiltInTypes.Datetime => GetModifierParameterDateTimeValue(obganismModifier, parameterIndex),
				NapBuiltInTypes.Duration => GetModifierParameterTimeSpanValue(obganismModifier, parameterIndex),

				_ => GetModifierParameterIntValue(obganismModifier, parameterIndex)
			};
		}

		static void ModifierParameterToIsInclusiveMeta(PartialFieldType fieldType, ObganismModifier obganismModifier, int parameterIndex, string metaName, bool defaultInclusivity) {
			var explicitInclusivity = GetModifierParameterStringValue(obganismModifier, parameterIndex);

			fieldType.Meta[metaName] = explicitInclusivity != null
				? explicitInclusivity.StartsWith("in", StringComparison.InvariantCultureIgnoreCase)
				: defaultInclusivity;
		}

		static void ModifierToAllowedValuesMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			if (obganismModifier.Parameters.Count != 0)
				fieldType.Meta[NapBuiltInMeta.AllowedValues] = fieldType.Name switch {
					NapBuiltInTypes.Int => GetModifierParameterValues(obganismModifier, GetModifierParameterIntValue),
					NapBuiltInTypes.Float => GetModifierParameterValues(obganismModifier, GetModifierParameterFloatValue),
					NapBuiltInTypes.Char => GetModifierParameterValues(obganismModifier, GetModifierParameterCharValue),
					NapBuiltInTypes.String => GetModifierParameterValues(obganismModifier, GetModifierParameterStringValue),
					NapBuiltInTypes.Date => GetModifierParameterValues(obganismModifier, GetModifierParameterDateValue),
					NapBuiltInTypes.Datetime => GetModifierParameterValues(obganismModifier, GetModifierParameterDateTimeValue),
					NapBuiltInTypes.Duration => GetModifierParameterValues(obganismModifier, GetModifierParameterTimeSpanValue),

					_ => GetModifierParameterValues(obganismModifier, GetModifierParameterValue)
				};
		}

		static void ModifierToForbiddenValuesMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			if (obganismModifier.Parameters.Count != 0)
				fieldType.Meta[NapBuiltInMeta.ForbiddenValues] = fieldType.Name switch {
					NapBuiltInTypes.Int => GetModifierParameterValues(obganismModifier, GetModifierParameterIntValue),
					NapBuiltInTypes.Float => GetModifierParameterValues(obganismModifier, GetModifierParameterFloatValue),
					NapBuiltInTypes.Char => GetModifierParameterValues(obganismModifier, GetModifierParameterCharValue),
					NapBuiltInTypes.String => GetModifierParameterValues(obganismModifier, GetModifierParameterStringValue),
					NapBuiltInTypes.Date => GetModifierParameterValues(obganismModifier, GetModifierParameterDateValue),
					NapBuiltInTypes.Datetime => GetModifierParameterValues(obganismModifier, GetModifierParameterDateTimeValue),
					NapBuiltInTypes.Duration => GetModifierParameterValues(obganismModifier, GetModifierParameterTimeSpanValue),

					_ => GetModifierParameterValues(obganismModifier, GetModifierParameterValue)
				};
		}

		static void ModifierToPatternMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			if (GetModifierParameterStringValue(obganismModifier, 0) is string pattern)
				fieldType.Meta[NapBuiltInMeta.Pattern] = new Regex(
					pattern,
					string.Equals(GetModifierParameterStringValue(obganismModifier, 1), "ignore case", StringComparison.InvariantCultureIgnoreCase)
						? RegexOptions.IgnoreCase
						: RegexOptions.None
				);
		}

		static void ModifierToSameAsMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			if (GetModifierParameterStringValue(obganismModifier, 0) is string stringValue)
				fieldType.Meta[NapBuiltInMeta.SameAs] = stringValue;
		}

		static void ModifierToNotSameAsMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			if (GetModifierParameterStringValue(obganismModifier, 0) is string stringValue)
				fieldType.Meta[NapBuiltInMeta.NotSameAs] = stringValue;
		}

		static void ModifierToDefaultMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
			fieldType.Meta[NapBuiltInMeta.Default] = fieldType.Name switch {
				NapBuiltInTypes.Bool => GetModifierParameterBoolValue(obganismModifier, 0),
				NapBuiltInTypes.Int => GetModifierParameterIntValue(obganismModifier, 0),
				NapBuiltInTypes.Float => GetModifierParameterFloatValue(obganismModifier, 0),
				NapBuiltInTypes.Char => GetModifierParameterCharValue(obganismModifier, 0),
				NapBuiltInTypes.String => GetModifierParameterStringValue(obganismModifier, 0),
				NapBuiltInTypes.Date => GetModifierParameterDateValue(obganismModifier, 0),
				NapBuiltInTypes.Datetime => GetModifierParameterDateTimeValue(obganismModifier, 0),
				NapBuiltInTypes.Duration => GetModifierParameterTimeSpanValue(obganismModifier, 0),

				_ => GetModifierParameterValue(obganismModifier, 0)
			};
		}

		//static void ModifierToCustomMeta(PartialFieldType fieldType, ObganismModifier obganismModifier) {
		//	if (obganismModifier.Parameters.Count == 0)
		//		fieldType.Meta[obganismModifier.Name] = true;

		//	else fieldType.Meta[obganismModifier.Name] = obganismModifier.Parameters[0] switch {
		//		ObganismModifierParameter.Integer integerParameter => integerParameter.Value,
		//		ObganismModifierParameter.Real realParameter => realParameter.Value,
		//		ObganismModifierParameter.String stringParameter => stringParameter.Value,
		//		ObganismModifierParameter.Name nameParameter => nameParameter.Value,
		//		ObganismModifierParameter.Type typeParameter => typeParameter.Value,

		//		_ => null
		//	};
		//}

		// Modifier Parameters
		// ----

		static IReadOnlyCollection<T> GetModifierParameterValues<T>(ObganismModifier obganismModifier, Func<ObganismModifier, int, T> getModifierParameterValue) {
			var values = new List<T>(obganismModifier.Parameters.Count);

			for (int i = 0; i < obganismModifier.Parameters.Count; ++i) {
				var value = getModifierParameterValue.Invoke(obganismModifier, i);

				if (value != null)
					values.Add(value);
			}

			return values;
		}

		static bool? GetModifierParameterBoolValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) is ObganismModifierParameter.Name { Value: var nameValue }
				? nameValue.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? true
				: nameValue.Equals("false", StringComparison.InvariantCultureIgnoreCase) ? false
				: null
				: null;

		static int? GetModifierParameterIntValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) switch {
				ObganismModifierParameter.Integer { Value: var intValue } => intValue,
				ObganismModifierParameter.Real { Value: var floatValue } => (int) floatValue,

				_ => null
			};

		static float? GetModifierParameterFloatValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) switch {
				ObganismModifierParameter.Real { Value: var floatValue } => floatValue,
				ObganismModifierParameter.Integer { Value: var intValue } => (float) intValue,

				_ => null
			};

		static char? GetModifierParameterCharValue(ObganismModifier obganismModifier, int parameterIndex) =>
			GetModifierParameterStringValue(obganismModifier, parameterIndex) is string stringValue && stringValue.Length != 0
				? stringValue[0]
				: null;

		static string? GetModifierParameterStringValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) switch {
				ObganismModifierParameter.String { Value: var stringValue } => stringValue,
				ObganismModifierParameter.Name { Value: var nameValue } => nameValue,

				_ => null
			};

		static DateTime? GetModifierParameterDateValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) is ObganismModifierParameter.String { Value: var stringValue } && TryParseDate(stringValue, out var dateValue)
				? dateValue
				: null;

		static DateTime? GetModifierParameterDateTimeValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) is ObganismModifierParameter.String { Value: var stringValue }
				? TryParseDateTime(stringValue, out var dateTimeValue) ? dateTimeValue
				: TryParseDate(stringValue, out var dateValue) ? dateValue
				: null
				: null;

		static TimeSpan? GetModifierParameterTimeSpanValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) is ObganismModifierParameter.String { Value: var stringValue } && TryParseTimeSpan(stringValue, out var timeSpanValue)
				? timeSpanValue
				: null;

		static object? GetModifierParameterValue(ObganismModifier obganismModifier, int parameterIndex) =>
			obganismModifier.Parameters.ElementAtOrDefault(parameterIndex) switch {
				ObganismModifierParameter.Integer { Value: var intValue } => intValue,
				ObganismModifierParameter.Real { Value: var realValue } => realValue,

				ObganismModifierParameter.Name { Value: var nameValue } =>
					  nameValue.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? true
					: nameValue.Equals("false", StringComparison.InvariantCultureIgnoreCase) ? false
					: nameValue,

				ObganismModifierParameter.String { Value: var stringValue } =>
					  TryParseDate(stringValue, out var dateValue) ? dateValue
					: TryParseDateTime(stringValue, out var dateTimeValue) ? dateTimeValue
					: TryParseTimeSpan(stringValue, out var durationValue) ? durationValue
					: stringValue,

				_ => null
			};

		// Date & Time Parsing
		// ----

		const string Iso8601Date = "yyyy-MM-dd";
		const string Iso8601DateTime = Iso8601Date + " HH:mm:ss";

		static bool TryParseDate(string @in, out DateTime @out) => DateTime.TryParseExact(@in, Iso8601Date, null, DateTimeStyles.None, out @out);
		static bool TryParseDateTime(string @in, out DateTime @out) => DateTime.TryParseExact(@in, Iso8601DateTime, null, DateTimeStyles.None, out @out);

		// Now, read it. You have 4 hours.
		// Really, it's just 4 blocks defining digits, eventual spaces, then either "days", "hours", "minutes" or "seconds",
		// With P and T messing around for a parody of compatibility with ISO 8601
		// Split it before each (?:\s*(\d+ for easier edition.
		static readonly Regex DurationPattern = new(@"^P?(?:(\d+)\s*d(?:ays?)?)?T?(?:\s*(\d+)\s*h(?:ours?)?)?(?:\s*(\d+)\s*m(?:in(?:utes?)?)?)?(?:\s*(\d+)\s*s(?:econds?)?)?$", RegexOptions.IgnoreCase);

		static bool TryParseTimeSpan(string @in, out TimeSpan @out) {
			if (string.IsNullOrWhiteSpace(@in)) {
				@out = default;
				return false;
			}

			var match = DurationPattern.Match(@in);

			if (match.Success) {
				var days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
				var hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
				var minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
				var seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

				@out = new TimeSpan(days, hours, minutes, seconds);
				return true;
			}

			else {
				@out = default;
				return false;
			}
		}
	}
}
