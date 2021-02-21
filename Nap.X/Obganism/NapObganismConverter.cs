using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nap.Obganism {
	public static class NapObganismConverter {
		/// <summary>
		/// Converts Nap container into Obganism source code representing a list of Obganism objects.
		/// </summary>
		///
		/// <param name="outputContainerMetaAsDetachedModifiers">
		/// If <c>true</c>, some metadata set on the container will be output as a leading, dettached modifier list.
		/// Defaults to <c>false</c>.
		///
		/// <list type="bullet">
		/// <item> The name of the container will be the first parameter of the <c>name</c> modifier. </item>
		/// </list>
		///
		/// Example:
		/// <code>
		/// -- (
		///    name("the container")
		/// )
		///
		/// cat {
		///    name : string -- in(1, 30)
		///    is grumpy : bool -- default(true)
		/// }
		/// </code>
		/// </param>
		public static string ContainerToObganism(Container container, bool outputContainerMetaAsDetachedModifiers = false) {
			if (container.Resources.Count == 0 && !outputContainerMetaAsDetachedModifiers)
				return string.Empty;

			var code = new StringBuilder();

			if (outputContainerMetaAsDetachedModifiers) {
				code.AppendLine("-- (");
				code.Append(@"   name(""");
				code.Append(container.Name);
				code.AppendLine(@""")");
				code.Append(')');
				code.AppendLine();
			}

			foreach (var resource in container.Resources)
				AppendResource(code, resource);

			return code.ToString();
		}

		public static string ResourceToObganism(Resource resource) {
			var code = new StringBuilder();

			AppendResource(code, resource);

			return code.ToString();
		}

		// ----

		static void AppendResource(StringBuilder code, Resource resource) {
			code.Append(resource.Name);

			if (resource.GenericTemplates.Count != 0) {
				code.Append(" of ");

				if (resource.GenericTemplates.Count == 1)
					code.Append(resource.GenericTemplates[0].Name);

				else {
					code.Append('(');
					code.Append(resource.GenericTemplates[0].Name);

					for (var i = 1; i < resource.GenericTemplates.Count; ++i) {
						code.Append(", ");
						code.Append(resource.GenericTemplates[i].Name);
					}

					code.Append(')');
				}
			}

			if (resource.Fields.Count != 0) {
				code.AppendLine(" {");

				foreach (var field in resource.Fields)
					AppendField(code, field);

				code.Append('}');
				code.AppendLine();
			}

			code.AppendLine();
		}

		static void AppendField(StringBuilder code, KeyValuePair<string, FieldType> field) {
			code.Append("   ");
			code.Append(field.Key);
			code.Append(" : ");
			code.AppendFieldType(field.Value);
			code.AppendFieldMeta(field.Value);
			code.AppendLine();
		}

		static void AppendFieldType(this StringBuilder code, FieldType fieldType) {
			var fieldTypeName = fieldType switch {
				BoolType => "bool",
				IntType => "int",
				FloatType => "float",
				CharType => "char",
				StringType => "string",
				DateType => "date",
				DatetimeType => "datetime",
				DurationType => "duration",

				SetType => "set",
				ListType => "list",
				MapType => "map",

				TemplateType template => template.Name,
				ResourceType resource => resource.Name,

				_ => throw new Exception("5008C3E9-A46A-465D-AE7B-683F519EBD58: This is a bug. Please report it at https://github.com/odepax/nap/issues.")
			};

			code.Append(fieldTypeName);

			if (fieldType is SetType setType) {
				code.Append(" of ");
				AppendFieldType(code, setType.ElementType);
			}

			else if (fieldType is ListType listType) {
				code.Append(" of ");
				AppendFieldType(code, listType.ElementType);
			}

			else if (fieldType is MapType mapType) {
				code.Append(" of (");
				AppendFieldType(code, mapType.KeyType);
				code.Append(", ");
				AppendFieldType(code, mapType.ElementType);
				code.Append(')');
			}

			else if (fieldType is ResourceType resourceType && resourceType.Generics.Count != 0) {
				code.Append(" of ");

				if (resourceType.Generics.Count == 1)
					AppendFieldType(code, resourceType.Generics[0]);

				else {
					code.Append('(');
					AppendFieldType(code, resourceType.Generics[0]);

					for (var i = 1; i < resourceType.Generics.Count; ++i) {
						code.Append(", ");
						code.AppendFieldType(resourceType.Generics[i]);
					}

					code.Append(')');
				}
			}
		}

		static void AppendFieldMeta(this StringBuilder code, FieldType fieldType) {
			var metaTargetGenericType = fieldType switch {
				ListType listType => listType.ElementType,
				SetType setType => setType.ElementType,
				MapType mapType => mapType.ElementType,

				ResourceType resourceType => resourceType.Generics.FirstOrDefault(),

				_ => null
			};

			if (fieldType.Meta.Count + (metaTargetGenericType?.Meta.Count ?? 0) != 0) {
				var modifierListStartPosition = code.Length;
				var minMaxDone = false;
				var modifierCount = 0;


				foreach (var meta in fieldType.Meta)
					(minMaxDone, modifierCount) = code.AppendMeta(meta, fieldType, minMaxDone, modifierCount);

				if (metaTargetGenericType != null) {
					minMaxDone = false;

					foreach (var meta in metaTargetGenericType.Meta)
						(minMaxDone, modifierCount) = code.AppendGenericMeta(meta, metaTargetGenericType, minMaxDone, modifierCount);
				}

				if (1 < modifierCount)
					code.Insert(modifierListStartPosition, " -- ( ");
				else if (0 < modifierCount)
					code.Insert(modifierListStartPosition, " -- ");

				if (1 < modifierCount)
					code.Append(" )");
			}
		}

		static (bool MinMaxDone, int ModifierCount) AppendMeta(this StringBuilder code, KeyValuePair<string, object?> meta, FieldType fieldType, bool minMaxDone, int modifierCount) {
			if (meta.Value != null) switch (meta.Key) {
				case NapBuiltInMeta.MinIsInclusive:
				case NapBuiltInMeta.MaxIsInclusive:
					break;

				case NapBuiltInMeta.Min:
				case NapBuiltInMeta.Max:
					if (!minMaxDone) {
						if (0 < modifierCount) code.Append(", ");
						AppendMinMax(code, fieldType);
						minMaxDone = true;
						++modifierCount;
					}
					break;

				case NapBuiltInMeta.AllowedValues:
					if (0 < modifierCount) code.Append(", ");
					code.Append("one of(");
					code.AppendMetaValue(meta.Value);
					code.Append(')');
					++modifierCount;
					break;

				case NapBuiltInMeta.ForbiddenValues:
					if (0 < modifierCount) code.Append(", ");
					code.Append("not one of(");
					code.AppendMetaValue(meta.Value);
					code.Append(')');
					++modifierCount;
					break;

				case NapBuiltInMeta.Pattern:
					if (meta.Value is Regex regexValue) {
						if (0 < modifierCount) code.Append(", ");
						code.Append(@"pattern(""");
						code.Append(regexValue.ToString());
						code.Append(regexValue.Options.HasFlag(RegexOptions.IgnoreCase) ? @""", ignore case)" : @""")");
						++modifierCount;
					}
					break;

				case NapBuiltInMeta.Default:
					if (0 < modifierCount) code.Append(", ");
					code.Append("default(");
					code.AppendMetaValue(meta.Value);
					code.Append(')');
					++modifierCount;
					break;

				case NapBuiltInMeta.AllowEmpty:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "allow empty" : "not empty");
					++modifierCount;
					break;

				case NapBuiltInMeta.AllowDuplicates:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "allow duplicates" : "no duplicates");
					++modifierCount;
					break;

				case NapBuiltInMeta.AllowMultiline:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "multiline" : "not multiline");
					++modifierCount;
					break;

				case NapBuiltInMeta.IsOptional:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "optional" : "not optional");
					++modifierCount;
					break;

				case NapBuiltInMeta.SameAs:
					if (0 < modifierCount) code.Append(", ");
					code.Append(@"same as(""");
					code.Append(meta.Value);
					code.Append(@""")");
					++modifierCount;
					break;

				case NapBuiltInMeta.NotSameAs:
					if (0 < modifierCount) code.Append(", ");
					code.Append(@"not same as(""");
					code.Append(meta.Value);
					code.Append(@""")");
					++modifierCount;
					break;

				case NapBuiltInMeta.SelfReference:
					switch (meta.Value) {
						case SelfReference.Forbid:
							if (0 < modifierCount) code.Append(", ");
							code.Append("not self");
							++modifierCount;
							break;
						case SelfReference.Enforce:
							if (0 < modifierCount) code.Append(", ");
							code.Append("self");
							++modifierCount;
							break;
					}
					break;
			}

			return (minMaxDone, modifierCount);
		}

		static (bool MinMaxDone, int ModifierCount) AppendGenericMeta(this StringBuilder code, KeyValuePair<string, object?> meta, FieldType genericType, bool minMaxDone, int modifierCount) {
			if (meta.Value != null) switch (meta.Key) {
				case NapBuiltInMeta.MinIsInclusive:
				case NapBuiltInMeta.MaxIsInclusive:
					break;

				case NapBuiltInMeta.Min:
				case NapBuiltInMeta.Max:
					if (!minMaxDone) {
						code.Append(0 < modifierCount ? ", all " : "all ");
						code.AppendMinMax(genericType);
						minMaxDone = true;
						++modifierCount;
					}
					break;

				case NapBuiltInMeta.AllowedValues:
					code.Append(0 < modifierCount ? ", all one of(" : "all one of(");
					code.AppendMetaValue(meta.Value);
					code.Append(')');
					++modifierCount;
					break;

				case NapBuiltInMeta.ForbiddenValues:
					code.Append(0 < modifierCount ? ", not any one of(" : "not any one of(");
					code.AppendMetaValue(meta.Value);
					code.Append(')');
					++modifierCount;
					break;

				case NapBuiltInMeta.Pattern:
					if (meta.Value is Regex regexValue) {
						code.Append(0 < modifierCount ? @", all pattern(""" : @"all pattern(""");
						code.Append(regexValue.ToString());
						code.Append(regexValue.Options.HasFlag(RegexOptions.IgnoreCase) ? @""", ignore case)" : @""")");
						++modifierCount;
					}
					break;

				case NapBuiltInMeta.AllowEmpty:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "any empty" : "not any empty");
					++modifierCount;
					break;

				case NapBuiltInMeta.AllowMultiline:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "any multiline" : "not any multiline");
					++modifierCount;
					break;

				case NapBuiltInMeta.IsOptional:
					if (0 < modifierCount) code.Append(", ");
					code.Append(Equals(meta.Value, true) ? "all optional" : "not any optional");
					++modifierCount;
					break;

				case NapBuiltInMeta.SameAs:
					code.Append(0 < modifierCount ? @", all same as(""" : @"all same as(""");
					code.Append(meta.Value);
					code.Append(@""")");
					++modifierCount;
					break;

				case NapBuiltInMeta.NotSameAs:
					code.Append(0 < modifierCount ? @", not any same as(""" : @"not any same as(""");
					code.Append(meta.Value);
					code.Append(@""")");
					++modifierCount;
					break;

				case NapBuiltInMeta.SelfReference:
					switch (meta.Value) {
						case SelfReference.Forbid:
							code.Append(0 < modifierCount ? ", not any self" : "not any self");
							++modifierCount;
							break;
						case SelfReference.Enforce:
							code.Append(0 < modifierCount ? ", all self" : "all self");
							++modifierCount;
							break;
					}
					break;
			}

			return (minMaxDone, modifierCount);
		}

		static void AppendMinMax(this StringBuilder code, FieldType fieldType) {
			var minPresent = fieldType.TryGetMeta(NapBuiltInMeta.Min, out object? min);
			var maxPresent = fieldType.TryGetMeta(NapBuiltInMeta.Max, out object? max);

			fieldType.TryGetMeta(NapBuiltInMeta.MinIsInclusive, out bool minInclusive, true);
			fieldType.TryGetMeta(NapBuiltInMeta.MaxIsInclusive, out bool maxInclusive, true);

			if (minPresent && maxPresent) {
				if (minInclusive == maxInclusive) {
					code.Append(minInclusive ? "in" : "between");
					code.Append('(');
					code.AppendMetaValue(min!);
					code.Append(", ");
					code.AppendMetaValue(max!);
					code.Append(')');
				}

				else {
					code.Append("between");
					code.Append('(');
					code.AppendMetaValue(min!);
					code.Append(minInclusive ? ", included, " : ", excluded, ");
					code.AppendMetaValue(max!);
					code.Append(maxInclusive ? ", included" : ", excluded");
					code.Append(')');
				}
			}

			else if (minPresent) {
				code.Append(minInclusive ? "min" : "above");
				code.Append('(');
				code.AppendMetaValue(min!);
				code.Append(')');
			}

			else { // Therefore maxPresent is true, assuming I don't call this methods for nothing...
				code.Append(maxInclusive ? "max" : "below");
				code.Append('(');
				code.AppendMetaValue(max!);
				code.Append(')');
			}
		}

		static void AppendMetaValue(this StringBuilder code, object value) {
			if (value is bool boolValue)
				code.Append(boolValue ? "true" : "false");

			else if (value is string or char) {
				code.Append('"');
				code.Append(value);
				code.Append('"');
			}

			else if (value is DateTime dateTimeValue)
				code.Append(dateTimeValue.ToString(dateTimeValue.TimeOfDay == TimeSpan.Zero
					? @"\""yyyy-MM-dd\"""
					: @"\""yyyy-MM-dd HH:mm:ss\"""
				));

			else if (value is TimeSpan timeSpanValue) {
				code.Append('"');

				if (timeSpanValue.Days != 0) code.Append(timeSpanValue.Days).Append('d');
				if (timeSpanValue.Hours != 0) code.Append(timeSpanValue.Hours).Append('h');
				if (timeSpanValue.Minutes != 0) code.Append(timeSpanValue.Minutes).Append('m');

				if (timeSpanValue.Seconds != 0 || timeSpanValue == TimeSpan.Zero)
					code.Append(timeSpanValue.Seconds).Append('s');

				code.Append('"');
			}

			else if (value is IEnumerable enumerableValue) {
				var enumerator = enumerableValue.GetEnumerator();

				if (enumerator.MoveNext()) {
					code.AppendMetaValue(enumerator.Current);

					while (enumerator.MoveNext()) {
						code.Append(", ");
						code.AppendMetaValue(enumerator.Current);
					}
				}
			}

			else code.Append(value);
		}
	}
}
