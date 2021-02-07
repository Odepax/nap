using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nap {
	static class EachEnumerableExtensions {
		public static IEnumerable<T> Each<T>(this IEnumerable<T> @this, Action<T> action) {
			foreach (var item in @this) {
				action.Invoke(item);

				yield return item;
			}
		}
	}

	sealed class MergingContext {
		string CurrentContainerName = string.Empty;
		string CurrentResourceName = string.Empty;
		string CurrentFieldName = string.Empty;

		// Contexts
		// ----

		public Context MergeContexts(IReadOnlyCollection<PartialContext> partials) {
			using var contextNameEnumerator = partials
				.Select(context => context.Name)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.Distinct()
				.GetEnumerator();

			if (!contextNameEnumerator.MoveNext())
				throw new NapContextMergingException($"Unnamed context.");

			var name = contextNameEnumerator.Current!; // Not null thanks to the above .Where() filter.

			if (contextNameEnumerator.MoveNext())
				throw new NapContextMergingException($"Different context names.");

			var containers = partials
				.SelectMany(context => context.Containers)
				.Each(container => {
					if (string.IsNullOrWhiteSpace(container.Name))
						throw new NapContextMergingException($"Unnamed container.");
				})
				.GroupBy(container => container.Name)
				.Select(containers => {
					CurrentContainerName = containers.Key!; // Already checked for null above.

					return MergeContainers(containers.ToList());
				})
				.ToList();

			return new Context {
				Name = name,
				Containers = containers
			};
		}

		// Containers
		// ----

		Container MergeContainers(IReadOnlyCollection<PartialContainer> partials) {
			var partialResources = partials
				.SelectMany(context => context.Resources)
				.Each(resource => {
					if (string.IsNullOrWhiteSpace(resource.Name))
						throw new NapContextMergingException($"Unnamed resource in container '{ CurrentContainerName }'.");
				})
				.ToLookup(resource => resource.Name);

			var resources = partialResources
				.Select(resources => {
					CurrentResourceName = resources.Key!; // Already checked for null above.

					return MergeResources(resources.ToList());
				})
				.ToList();

			var resourceDictionary = resources.ToDictionary(resource => resource.Name);

			foreach (var resource in resources) {
				CurrentResourceName = resource.Name;

				var partialFields = partialResources[resource.Name]
					.SelectMany(resource => resource.Fields)
					.Each(field => {
						if (string.IsNullOrWhiteSpace(field.Key))
							throw new NapContextMergingException($"Unnamed field in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");
					})
					.GroupBy(field => field.Key, field => field.Value);

				foreach (var field in partialFields) {
					CurrentFieldName = field.Key;

					((Dictionary<string, FieldType>) resource.Fields)[CurrentFieldName] = MergeResourceFields(field.ToList(), resourceDictionary);
				}
			}

			return new Container {
				Name = CurrentContainerName,
				Resources = resources
			};
		}

		// Top-Level Resources
		// ----
		// NO! You can't just replace regions with comments and decorations!
		// Dashes go: Brrr...

		Resource MergeResources(IReadOnlyCollection<PartialResource> partials) {
			var resourceName = CurrentResourceName.Trim().ToLower();

			switch (resourceName) {
				case "bool":
				case "int":
				case "float":
				case "char":
				case "string":
				case "date":
				case "datetime":
				case "duration":
				case "set":
				case "list":
				case "map":
					throw new NapContextMergingException($"Overriding built-in resource '{ resourceName }' is denied.");
			}

			var templateDictionary = partials
				.SelectMany(resource => resource.GenericTemplates)
				.Each(template => {
					if (template.Key < 0)
						throw new NapContextMergingException($"Negative generic template index [{ template.Key }] for resource '{ CurrentResourceName }' in container '{ CurrentContainerName }'.");
				})
				.GroupBy(template => template.Key, template => template.Value)
				.Select(template => {
					using var templateNameEnumerator = template.Distinct().GetEnumerator();

					if (!templateNameEnumerator.MoveNext()) // This is what .GroupBy() is supposed to prevent.
						throw new Exception("F6DCD8F1-F2E4-41D2-8E28-01159B52BF3B: This is a bug. Please report it at https://github.com/odepax/nap/issues.");

					var name = templateNameEnumerator.Current!;

					if (templateNameEnumerator.MoveNext())
						throw new NapContextMergingException($"Different generic templates [{ template.Key }] for resource '{ CurrentResourceName }' in container '{ CurrentContainerName }'.");

					return (Index: template.Key, Name: name);
				})
				.ToDictionary(template => template.Index, template => template.Name);

			var templates = MergeResourceTemplates(templateDictionary);

			return new Resource {
				Name = CurrentResourceName,
				GenericTemplates = templates,
				Fields = new Dictionary<string, FieldType>(0)
			};
		}

		IReadOnlyList<TemplateType> MergeResourceTemplates(IReadOnlyDictionary<int, string> partials) {
			if (partials.Count == 0)
				return Array.Empty<TemplateType>();

			var templateCount = partials.Max(template => template.Key) + 1;
			var templateTypes = new TemplateType[templateCount];

			var knownTemplateNames = new HashSet<string>();
			for (int i = 0; i < templateTypes.Length; ++i) {
				if (!partials.TryGetValue(i, out var templateName))
					throw new NapContextMergingException($"Missing generic template [{ i }] for resource '{ CurrentResourceName }' in container '{ CurrentContainerName }'.");

				else if (!knownTemplateNames.Add(templateName))
					throw new NapContextMergingException($"Duplicated generic template '{ templateName }' for resource '{ CurrentResourceName }' in container '{ CurrentContainerName }'.");
				
				else if (string.IsNullOrWhiteSpace(templateName))
					throw new NapContextMergingException($"Unnamed generic template [{ i }] for resource '{ CurrentResourceName }' in container '{ CurrentContainerName }'.");

				else templateTypes[i] = new TemplateType { Name = templateName };
			}

			return templateTypes;
		}

		// Resource Fields
		// ----

		FieldType MergeResourceFields(IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			using var fieldTypeNameEnumerator = partials
				.Select(context => context.Name)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.Distinct()
				.GetEnumerator();

			if (!fieldTypeNameEnumerator.MoveNext())
				throw new NapContextMergingException($"Unnamed type for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

			var fieldTypeName = fieldTypeNameEnumerator.Current!; // Not null thanks to the above .Where() filter.

			if (fieldTypeNameEnumerator.MoveNext())
				throw new NapContextMergingException($"Different types for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

			return MergeFieldTypes(fieldTypeName, partials, resources);
		}

		FieldType MergeFieldTypes(string fieldTypeName, IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) =>
			fieldTypeName switch {
				"bool" => MergeBoolFieldTypes(partials),
				"int" => MergeIntFieldTypes(partials),
				"float" => MergeFloatFieldTypes(partials),
				"char" => MergeCharFieldTypes(partials),
				"string" => MergeStringFieldTypes(partials),
				"date" => MergeDateFieldTypes(partials),
				"datetime" => MergeDatetimeFieldTypes(partials),
				"duration" => MergeDurationFieldTypes(partials),

				"set" => MergeSetFieldTypes(partials, resources),
				"list" => MergeListFieldTypes(partials, resources),
				"map" => MergeMapFieldTypes(partials, resources),

				_ => resources[CurrentResourceName].GenericTemplates.Any(template => template.Name == fieldTypeName)
					? MergeTemplateFieldTypes(fieldTypeName, partials)
					: MergeResourceFieldTypes(fieldTypeName, partials, resources)
			};

		// Primitive Types
		// ----

		BoolType MergeBoolFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, BoolTypeMetaConstraints) };
		}

		IntType MergeIntFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, IntTypeMetaConstraints) };
		}

		FloatType MergeFloatFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, FloatTypeMetaConstraints) };
		}

		CharType MergeCharFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, CharTypeMetaConstraints) };
		}

		StringType MergeStringFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, StringTypeMetaConstraints) };
		}

		DateType MergeDateFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, DateTypeMetaConstraints) };
		}

		DatetimeType MergeDatetimeFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, DatetimeTypeMetaConstraints) };
		}

		DurationType MergeDurationFieldTypes(IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Meta = MergeMeta(partials, DurationTypeMetaConstraints) };
		}

		// Collections
		// ----

		SetType MergeSetFieldTypes(IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var elementType = MergeFieldGenerics(1, partials, resources)[0];

			return new(elementType) { Meta = MergeMeta(partials, SetTypeMetaConstraints) };
		}

		ListType MergeListFieldTypes(IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var elementType = MergeFieldGenerics(1, partials, resources)[0];

			return new(elementType) { Meta = MergeMeta(partials, ListTypeMetaConstraints) };
		}

		MapType MergeMapFieldTypes(IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var genericTypes = MergeFieldGenerics(2, partials, resources);

			return new(genericTypes[0], genericTypes[1]) { Meta = MergeMeta(partials, MapTypeMetaConstraints) };
		}

		// Template References
		// ----

		TemplateType MergeTemplateFieldTypes(string fieldTypeName, IReadOnlyCollection<PartialFieldType> partials) {
			AssertNoGeneric(partials);

			return new() { Name = fieldTypeName, Meta = MergeMeta(partials, TemplateTypeMetaConstraints) };
		}

		// Resource References
		// ----

		ResourceType MergeResourceFieldTypes(string fieldTypeName, IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var genericTypes = MergeFieldGenerics(fieldTypeName, partials, resources);

			return new() { Name = fieldTypeName, Generics = genericTypes, Meta = MergeMeta(partials, ResourceTypeMetaConstraints) };
		}

		// Field Generics
		// ----

		void AssertNoGeneric(IReadOnlyCollection<PartialFieldType> partials) {
			if (partials.Any(type => type.Generics.Count != 0))
				throw new NapContextMergingException($"Extra generic [0] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName}' of container '{ CurrentContainerName }'.");
		}

		IReadOnlyList<FieldType> MergeFieldGenerics(string fieldTypeName, IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			if (resources.TryGetValue(fieldTypeName, out var linkedResource)) {
				var expectedGenericCount = linkedResource.GenericTemplates.Count;

				return MergeFieldGenerics(expectedGenericCount, partials, resources);
			}

			else {
				var generics = partials
					.SelectMany(type => type.Generics)
					.Each(generic => {
						if (generic.Key < 0)
							throw new NapContextMergingException($"Negative generic index [{ generic.Key }] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");
					})
					.ToLookup(generic => generic.Key, generic => generic.Value);

				var expectedGenericCount = generics.Count;

				return MergeFieldGenerics(expectedGenericCount, generics, resources);
			}
		}

		IReadOnlyList<FieldType> MergeFieldGenerics(int expectedGenericCount, IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var generics = partials
				.SelectMany(type => type.Generics)
				.Each(generic => {
					if (generic.Key < 0)
						throw new NapContextMergingException($"Negative generic index [{ generic.Key }] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

					else if (expectedGenericCount <= generic.Key)
						throw new NapContextMergingException($"Extra generic [{ generic.Key }] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");
				})
				.ToLookup(generic => generic.Key, generic => generic.Value);

			return MergeFieldGenerics(expectedGenericCount, generics, resources);
		}

		IReadOnlyList<FieldType> MergeFieldGenerics(int expectedGenericCount, ILookup<int, PartialFieldType> partials, IReadOnlyDictionary<string, Resource> resources) {
			var generics = new FieldType[expectedGenericCount];

			for (int i = 0; i < generics.Length; ++i) {
				if (partials.Contains(i)) {
					var generic = partials[i];
					using var genericTypeNameEnumerator = generic
						.Where(generic => !string.IsNullOrWhiteSpace(generic.Name))
						.Select(generic => generic.Name!) // Not null thanks to .Where() check.
						.Distinct()
						.GetEnumerator();

					if (!genericTypeNameEnumerator.MoveNext())
						throw new NapContextMergingException($"Unnamed type for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

					var genericTypeName = genericTypeNameEnumerator.Current;

					if (genericTypeNameEnumerator.MoveNext())
						throw new NapContextMergingException($"Different generics [{ i }] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

					generics[i] = MergeFieldTypes(genericTypeName, generic.ToList(), resources);
				}

				else throw new NapContextMergingException($"Missing generic [{ i }] for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");
			}

			return generics;
		}

		// Meta
		// ----

		IReadOnlyDictionary<string, object?> MergeMeta(IReadOnlyCollection<PartialFieldType> partials, IReadOnlyDictionary<string, Predicate<object?>> metaConstraints) {
			var mergedMeta = new Dictionary<string, object?>();

			foreach (var (name, value) in partials.SelectMany(type => type.Meta)) {
				// Not the first time a value is set for this meta: check the values are the same.
				if (mergedMeta.TryGetValue(name, out var existingValue)) {
					if (!Equals(value, existingValue))
						throw new NapContextMergingException($"Different meta '{ name }' for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");
				}

				// First time we see a value for this meta: perform checks if built-in.
				else if (metaConstraints.TryGetValue(name, out var predicate) && !predicate.Invoke(value))
					throw new NapContextMergingException($"Wrong meta value '{ name }' for field '{ CurrentFieldName }' in resource '{ CurrentResourceName }' of container '{ CurrentContainerName }'.");

				// Unknown "extra" meta.
				else mergedMeta[name] = value;
			}

			return mergedMeta;
		}

		static readonly Predicate<object?> ValueIsBool = value => value is bool;
		static readonly Predicate<object?> ValueIsInt = value => value is int;
		static readonly Predicate<object?> ValueIsFloat = value => value is float;
		static readonly Predicate<object?> ValueIsChar = value => value is char;
		static readonly Predicate<object?> ValueIsString = value => value is string;
		static readonly Predicate<object?> ValueIsDateTime = value => value is DateTime;
		static readonly Predicate<object?> ValueIsTimeSpan = value => value is TimeSpan;

		static readonly Predicate<object?> ValueIsRegex = value => value is Regex;
		static readonly Predicate<object?> ValueIsSelfReference = value => value is SelfReference;

		static readonly Predicate<object?> ValueIsCollectionOfInt = value => value is IReadOnlyCollection<int>;
		static readonly Predicate<object?> ValueIsCollectionOfFloat = value => value is IReadOnlyCollection<float>;
		static readonly Predicate<object?> ValueIsCollectionOfChar = value => value is IReadOnlyCollection<char>;
		static readonly Predicate<object?> ValueIsCollectionOfString = value => value is IReadOnlyCollection<string>;
		static readonly Predicate<object?> ValueIsCollectionOfDateTime = value => value is IReadOnlyCollection<DateTime>;
		static readonly Predicate<object?> ValueIsCollectionOfTimeSpan = value => value is IReadOnlyCollection<TimeSpan>;

		static readonly IReadOnlyDictionary<string, Predicate<object?>> TemplateTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> ResourceTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.IsOptional] = ValueIsBool,
			[NapBuiltInMeta.SelfReference] = ValueIsSelfReference,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> BoolTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.Default] = ValueIsBool,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> IntTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfInt,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfInt,

			[NapBuiltInMeta.Min] = ValueIsInt,
			[NapBuiltInMeta.Max] = ValueIsInt,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsInt,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> FloatTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfFloat,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfFloat,

			[NapBuiltInMeta.Min] = ValueIsFloat,
			[NapBuiltInMeta.Max] = ValueIsFloat,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsFloat,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> CharTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfChar,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfChar,

			[NapBuiltInMeta.Default] = ValueIsChar,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> StringTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfString,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfString,
			[NapBuiltInMeta.Pattern] = ValueIsRegex,

			[NapBuiltInMeta.AllowEmpty] = ValueIsBool,
			[NapBuiltInMeta.AllowMultiline] = ValueIsBool,

			[NapBuiltInMeta.Min] = ValueIsInt,
			[NapBuiltInMeta.Max] = ValueIsInt,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsString,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> DateTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfDateTime,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfDateTime,

			[NapBuiltInMeta.Min] = ValueIsDateTime,
			[NapBuiltInMeta.Max] = ValueIsDateTime,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsDateTime,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> DatetimeTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfDateTime,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfDateTime,

			[NapBuiltInMeta.Min] = ValueIsDateTime,
			[NapBuiltInMeta.Max] = ValueIsDateTime,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsDateTime,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> DurationTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowedValues] = ValueIsCollectionOfTimeSpan,
			[NapBuiltInMeta.ForbiddenValues] = ValueIsCollectionOfTimeSpan,

			[NapBuiltInMeta.Min] = ValueIsTimeSpan,
			[NapBuiltInMeta.Max] = ValueIsTimeSpan,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.Default] = ValueIsTimeSpan,
			[NapBuiltInMeta.IsOptional] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> SetTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowDuplicates] = ValueIsBool,
			[NapBuiltInMeta.AllowEmpty] = ValueIsBool,

			[NapBuiltInMeta.Min] = ValueIsInt,
			[NapBuiltInMeta.Max] = ValueIsInt,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> ListTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowDuplicates] = ValueIsBool,
			[NapBuiltInMeta.AllowEmpty] = ValueIsBool,

			[NapBuiltInMeta.Min] = ValueIsInt,
			[NapBuiltInMeta.Max] = ValueIsInt,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};

		static readonly IReadOnlyDictionary<string, Predicate<object?>> MapTypeMetaConstraints = new Dictionary<string, Predicate<object?>> {
			[NapBuiltInMeta.AllowDuplicates] = ValueIsBool,
			[NapBuiltInMeta.AllowEmpty] = ValueIsBool,

			[NapBuiltInMeta.Min] = ValueIsInt,
			[NapBuiltInMeta.Max] = ValueIsInt,
			[NapBuiltInMeta.MinIsInclusive] = ValueIsBool,
			[NapBuiltInMeta.MaxIsInclusive] = ValueIsBool,

			[NapBuiltInMeta.SameAs] = ValueIsString,
			[NapBuiltInMeta.NotSameAs] = ValueIsString
		};
	}
}
