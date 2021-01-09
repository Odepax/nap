using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Nap {
	static class StdConstraints {
		public const string MIN = "min";
		public const string MAX = "max";
		public const string MINISINCLUSIVE = "minInclusive";
		public const string MAXISINCLUSIVE = "maxInclusive";
		public const string EXCLUSIONMIN = "exclusionMin";
		public const string EXCLUSIONMAX = "exclusionMax";
		public const string EXCLUSIONMINISINCLUSIVE = "exclusionMinInclusive";
		public const string EXCLUSIONMAXISINCLUSIVE = "exclusionMaxInclusive";
		public const string DEFAULT = "default";
		public const string ISOPTIONAL = "optional";
		public const string ALLOWEDVALUES = "allowedValues";
		public const string FORBIDDENVALUES = "forbiddenValues";
		public const string ALLOWSEMPTY = "empty";
		public const string ALLOWSDUPLICATES = "duplicates";
		public const string PATTERN = "pattern";
		public const string ALLOWSMULTILINE = "multiline";
		public const string SAMEAS = "same";
		public const string NOTSAMEAS = "notSame";
		public const string SELFREFERENCE = "self";
	}

	class ContextBuilder : IContextBuilder {
		public string Name { get; set; } = string.Empty;
		public readonly List<ContainerBuilder> Containers = new();

		public void AddContainer(Action<IContainerBuilder> init) {
			var container = new ContainerBuilder();

			init(container);

			Containers.Add(container);
		}

		public Context Build() => BuildContext();

		public Context BuildContext() => new() {
			Name = Name,
			Containers = Containers
				.GroupBy(container => container.Name)
				.Select(BuildContainer)
				.ToList()
		};

		static Container BuildContainer(IGrouping<string, ContainerBuilder> containerGroup) {
			var resourceGroups = containerGroup
				.SelectMany(container => container.Resources)
				.ToLookup(resource => resource.Name);

			var container = new Container() {
				Name = containerGroup.Key,
				Resources = resourceGroups
					.Select(resourceGroup => BuildResource(containerGroup.Key, resourceGroup))
					.ToList()
			};

			var resourceDictionary = container.Resources.ToDictionary(resource => resource.Name);

			foreach (var linkedResource in container.Resources) {
				var linkedResourceFields = (Dictionary<string, DataType>) linkedResource.Fields;

				var fieldGroups = resourceGroups[linkedResource.Name]
					.SelectMany(resource => resource.Fields)
					.GroupBy(field => field.Name, field => field.Type);

				foreach (var fieldGroup in fieldGroups)
					linkedResourceFields[fieldGroup.Key] = BuildResourceField(linkedResource.Name, containerGroup.Key, fieldGroup, resourceDictionary);
			}

			return container;
		}

		static Resource BuildResource(string containerName, IGrouping<string, ResourceTypeBuilder> resourceGroup) {
			switch (resourceGroup.Key) {
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
					throw new NapContextException($"Overriding built-in type '{ resourceGroup.Key }' is denied.");
			}

			return new() {
				Name = resourceGroup.Key,
				GenericTemplates = BuildResourceGenericTemplates(containerName, resourceGroup)
			};
		}

		static TemplateType[] BuildResourceGenericTemplates(string containerName, IGrouping<string, ResourceTypeBuilder> resourceGroup) {
			var genericTemplateCount = resourceGroup.Max(resource => resource.MaxGenericTemplateIndex) + 1;
			var genericTemplates = new TemplateType[genericTemplateCount];

			foreach (var (i, template) in resourceGroup.SelectMany(type => type.GenericTemplates)) {
				if (genericTemplates[i] == default)
					genericTemplates[i] = new TemplateType { Name = template };

				else if (genericTemplates[i].Name != template)
					throw new NapContextException($"Different generic templates [{ i }] for type '{ resourceGroup.Key }' in container '{ containerName }'.");
			}

			var knownTemplates = new HashSet<string>();
			for (int i = 0; i < genericTemplates.Length; ++i) {
				if (genericTemplates[i] == default)
					throw new NapContextException($"Missing generic template [{ i }] for type '{ resourceGroup.Key }' in container '{ containerName }'.");

				else if (!knownTemplates.Add(genericTemplates[i].Name))
					throw new NapContextException($"Duplicated generic template '{ genericTemplates[i].Name }' for type '{ resourceGroup.Key }' in container '{ containerName }'.");
			}

			return genericTemplates;
		}

		static DataType BuildResourceField(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var fieldTypeName = string.Empty;

			foreach (var name in fieldGroup.Select(type => type.Name).Where(name => name.Length != 0)) {
				if (fieldTypeName == string.Empty)
					fieldTypeName = name;

				else if (fieldTypeName != name)
					throw new NapContextException($"Different types for field '{ fieldGroup.Key }' in type '{ resourceName }' of container '{ containerName }'.");
			}

			return BuildFieldType(resourceName, containerName, fieldGroup, resources, fieldTypeName);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static DataType BuildFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources, string fieldTypeName) =>
			fieldTypeName switch {
				"bool" => BuildBoolFieldType(resourceName, containerName, fieldGroup),
				"int" => BuildIntFieldType(resourceName, containerName, fieldGroup),
				"float" => BuildFloatFieldType(resourceName, containerName, fieldGroup),
				"char" => BuildCharFieldType(resourceName, containerName, fieldGroup),
				"string" => BuildStringFieldType(resourceName, containerName, fieldGroup),
				"date" => BuildDateFieldType(resourceName, containerName, fieldGroup),
				"datetime" => BuildDatetimeFieldType(resourceName, containerName, fieldGroup),
				"duration" => BuildDurationFieldType(resourceName, containerName, fieldGroup),

				"set" => BuildSetFieldType(resourceName, containerName, fieldGroup, resources),
				"list" => BuildListFieldType(resourceName, containerName, fieldGroup, resources),
				"map" => BuildMapFieldType(resourceName, containerName, fieldGroup, resources),

				_ => resources[resourceName].GenericTemplates.Any(template => template.Name == fieldTypeName)
					? BuildTemplateFieldType(resourceName, containerName, fieldTypeName, fieldGroup)
					: BuildResourceFieldType(resourceName, containerName, fieldTypeName, fieldGroup, resources)
			};

		static IReadOnlyList<DataType> BuildFieldGenerics(string resourceName, string containerName, string fieldTypeName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			if (resources.TryGetValue(fieldTypeName, out var linkedResource)) {
				var expectedGenericCount = linkedResource.GenericTemplates.Count;

				return BuildFieldGenerics(resourceName, containerName, expectedGenericCount, fieldGroup, resources);
			}

			else {
				var genericGroups = fieldGroup
					.SelectMany(type => type.Generics)
					.ToLookup(generic => generic.Index, generic => generic.Type);

				var expectedGenericCount = genericGroups.Count;

				return BuildFieldGenerics(resourceName, containerName, expectedGenericCount, genericGroups, fieldGroup, resources);
			}
		}

		static IReadOnlyList<DataType> BuildFieldGenerics(string resourceName, string containerName, int expectedGenericCount, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var genericGroups = fieldGroup
				.SelectMany(type => type.Generics)
				.Select(generic => {
					if (expectedGenericCount <= generic.Index)
						throw new NapContextException($"Extra generic [{ generic.Index }] for field '{ fieldGroup.Key }' in type '{ resourceName }' of container '{ containerName }'.");

					else return generic;
				})
				.ToLookup(generic => generic.Index, generic => generic.Type);

			return BuildFieldGenerics(resourceName, containerName, expectedGenericCount, genericGroups, fieldGroup, resources);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static DataType[] BuildFieldGenerics(string resourceName, string containerName, int expectedGenericCount, ILookup<int, FieldTypeBuilder> genericGroups, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var generics = new DataType[expectedGenericCount];

			for (int i = 0; i < expectedGenericCount; ++i) {
				if (genericGroups.Contains(i)) {
					var generic = genericGroups[i];
					var genericTypeName = string.Empty;

					foreach (var name in generic.Select(type => type.Name).Where(name => name.Length != 0)) {
						if (genericTypeName == string.Empty)
							genericTypeName = name;

						else if (genericTypeName != name)
							throw new NapContextException($"Different generics [{ i }] for field '{ fieldGroup.Key }' in type '{ resourceName }' of container '{ containerName }'.");
					}

					generics[i] = BuildFieldType(resourceName, containerName, new FieldGenericGrouping(fieldGroup.Key, generic), resources, genericTypeName);
				}

				else throw new NapContextException($"Missing generic [{ i }] for field '{ fieldGroup.Key }' in type '{ resourceName }' of container '{ containerName }'.");
			}

			return generics;
		}

		class FieldGenericGrouping : IGrouping<string, FieldTypeBuilder> {
			public string Key { get; }
			readonly IEnumerable<FieldTypeBuilder> Elements;

			public FieldGenericGrouping(string key, IEnumerable<FieldTypeBuilder> elements) {
				Key = key;
				Elements = elements;
			}

			public IEnumerator<FieldTypeBuilder> GetEnumerator() => Elements.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
		}

		static T BuildConstraint<T>(string constraintName, T fallback, string fieldName, string resourceName, string containerName, ILookup<string, object?> constraints) {
			var constraintValue = fallback;

			foreach (var value in constraints[constraintName]) {
				if (value is T typedValue) {
					if (Equals(constraintValue, fallback))
						constraintValue = typedValue;

					else if (!Equals(constraintValue, typedValue))
						throw new NapContextException($"Different constraints '{ constraintName }' for field '{ fieldName }' in type '{ resourceName }' of container '{ containerName }'.");
				}

				else throw new NapContextException($"Wrong constraint value '{ constraintName }' for field '{ fieldName }' in type '{ resourceName }' of container '{ containerName }'.");
			}

			return constraintValue;
		}

		static ResourceType BuildResourceFieldType(string resourceName, string containerName, string fieldTypeName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				Name = fieldTypeName,
				Generics = BuildFieldGenerics(resourceName, containerName, fieldTypeName, fieldGroup, resources),

				SelfReference = BuildConstraint<bool?>(StdConstraints.SELFREFERENCE, null, fieldGroup.Key, resourceName, containerName, constraints) switch {
					true => SelfReference.Enforce,
					false => SelfReference.Forbid,
					_ => SelfReference.Allow
				},
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AssertNoGeneric(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			if (fieldGroup.Any(type => type.Generics.Count != 0))
				throw new NapContextException($"Extra generic [0] for field '{ fieldGroup.Key }' in type '{ resourceName }' of container '{ containerName }'.");
		}

		static TemplateType BuildTemplateFieldType(string resourceName, string containerName, string fieldTypeName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				Name = fieldTypeName,

				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static BoolType BuildBoolFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				Default = BuildConstraint<bool?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static IntType BuildIntFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<int>>(StdConstraints.ALLOWEDVALUES, ImmutableList<int>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<int>>(StdConstraints.FORBIDDENVALUES, ImmutableList<int>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<int?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<int?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<int?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<int?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<int?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static FloatType BuildFloatFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<float>>(StdConstraints.ALLOWEDVALUES, ImmutableList<float>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<float>>(StdConstraints.FORBIDDENVALUES, ImmutableList<float>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<float?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<float?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<float?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<float?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<float?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static CharType BuildCharFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<char>>(StdConstraints.ALLOWEDVALUES, ImmutableList<char>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<char>>(StdConstraints.FORBIDDENVALUES, ImmutableList<char>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<char?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static StringType BuildStringFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<string>>(StdConstraints.ALLOWEDVALUES, ImmutableList<string>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<string>>(StdConstraints.FORBIDDENVALUES, ImmutableList<string>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Pattern = BuildConstraint<Regex?>(StdConstraints.PATTERN, null, fieldGroup.Key, resourceName, containerName, constraints),
				AllowsEmpty = BuildConstraint<bool>(StdConstraints.ALLOWSEMPTY, true, fieldGroup.Key, resourceName, containerName, constraints),
				AllowsMultiline = BuildConstraint<bool>(StdConstraints.ALLOWSMULTILINE, false, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<int?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<int?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<int?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<int?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<string?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static DateType BuildDateFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<DateTime>>(StdConstraints.ALLOWEDVALUES, ImmutableList<DateTime>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<DateTime>>(StdConstraints.FORBIDDENVALUES, ImmutableList<DateTime>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<DateTime?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<DateTime?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<DateTime?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<DateTime?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<DateTime?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static DatetimeType BuildDatetimeFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<DateTime>>(StdConstraints.ALLOWEDVALUES, ImmutableList<DateTime>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<DateTime>>(StdConstraints.FORBIDDENVALUES, ImmutableList<DateTime>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<DateTime?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<DateTime?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<DateTime?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<DateTime?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<DateTime?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static DurationType BuildDurationFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup) {
			AssertNoGeneric(resourceName, containerName, fieldGroup);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new() {
				AllowedValues = BuildConstraint<IReadOnlyCollection<TimeSpan>>(StdConstraints.ALLOWEDVALUES, ImmutableList<TimeSpan>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				ForbiddenValues = BuildConstraint<IReadOnlyCollection<TimeSpan>>(StdConstraints.FORBIDDENVALUES, ImmutableList<TimeSpan>.Empty, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<TimeSpan?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<TimeSpan?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<TimeSpan?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<TimeSpan?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Default = BuildConstraint<TimeSpan?>(StdConstraints.DEFAULT, null, fieldGroup.Key, resourceName, containerName, constraints),
				IsOptional = BuildConstraint<bool>(StdConstraints.ISOPTIONAL, false, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static SetType BuildSetFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var elementType = BuildFieldGenerics(resourceName, containerName, 1, fieldGroup, resources)[0];

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new(elementType) {
				AllowsDuplicates = BuildConstraint<bool>(StdConstraints.ALLOWSDUPLICATES, true, fieldGroup.Key, resourceName, containerName, constraints),
				AllowsEmpty = BuildConstraint<bool>(StdConstraints.ALLOWSEMPTY, true, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<int?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<int?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<int?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<int?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static ListType BuildListFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var elementType = BuildFieldGenerics(resourceName, containerName, 1, fieldGroup, resources)[0];

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new(elementType) {
				AllowsDuplicates = BuildConstraint<bool>(StdConstraints.ALLOWSDUPLICATES, true, fieldGroup.Key, resourceName, containerName, constraints),
				AllowsEmpty = BuildConstraint<bool>(StdConstraints.ALLOWSEMPTY, true, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<int?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<int?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<int?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<int?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}

		static MapType BuildMapFieldType(string resourceName, string containerName, IGrouping<string, FieldTypeBuilder> fieldGroup, IReadOnlyDictionary<string, Resource> resources) {
			var genericTypes = BuildFieldGenerics(resourceName, containerName, 2, fieldGroup, resources);

			var constraints = fieldGroup
				.SelectMany(type => type.Constraints)
				.ToLookup(constraint => constraint.Name, constraint => constraint.Value);

			return new(genericTypes[0], genericTypes[1]) {
				AllowsDuplicates = BuildConstraint<bool>(StdConstraints.ALLOWSDUPLICATES, true, fieldGroup.Key, resourceName, containerName, constraints),
				AllowsEmpty = BuildConstraint<bool>(StdConstraints.ALLOWSEMPTY, true, fieldGroup.Key, resourceName, containerName, constraints),
				Min = BuildConstraint<int?>(StdConstraints.MIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				MinIsInclusive = BuildConstraint<bool>(StdConstraints.MINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				Max = BuildConstraint<int?>(StdConstraints.MAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				MaxIsInclusive = BuildConstraint<bool>(StdConstraints.MAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMin = BuildConstraint<int?>(StdConstraints.EXCLUSIONMIN, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMinIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMINISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMax = BuildConstraint<int?>(StdConstraints.EXCLUSIONMAX, null, fieldGroup.Key, resourceName, containerName, constraints),
				ExclusionMaxIsInclusive = BuildConstraint<bool>(StdConstraints.EXCLUSIONMAXISINCLUSIVE, true, fieldGroup.Key, resourceName, containerName, constraints),
				SameAs = BuildConstraint<string?>(StdConstraints.SAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints),
				NotSameAs = BuildConstraint<string?>(StdConstraints.NOTSAMEAS, null, fieldGroup.Key, resourceName, containerName, constraints)
			};
		}
	}

	class ContainerBuilder : IContainerBuilder {
		public readonly List<ResourceTypeBuilder> Resources = new();

		public string Name { get; set; } = string.Empty;

		public void AddResource(Action<IResourceTypeBuilder> init) {
			var type = new ResourceTypeBuilder();

			init(type);

			Resources.Add(type);
		}
	}

	class ResourceTypeBuilder : IResourceTypeBuilder {
		public int MaxGenericTemplateIndex = -1;
		public readonly List<(int Index, string Name)> GenericTemplates = new();
		public readonly List<(string Name, FieldTypeBuilder Type)> Fields = new();

		public string Name { get; set; } = string.Empty;

		public void SetGenericTemplate(int i, string genericName) {
			GenericTemplates.Add((i, genericName));

			if (MaxGenericTemplateIndex < i)
				MaxGenericTemplateIndex = i;
		}

		public void SetFieldType(string fieldName, Action<IFieldTypeBuilder> typeInit) {
			var fieldType = new FieldTypeBuilder();

			typeInit(fieldType);

			Fields.Add((fieldName, fieldType));
		}
	}

	class FieldTypeBuilder : IFieldTypeBuilder {
		public readonly List<(int Index, FieldTypeBuilder Type)> Generics = new();
		public readonly List<(string Name, object? Value)> Constraints = new();

		public string Name { get; set; } = string.Empty;

		public void SetGeneric(int i, Action<IFieldTypeBuilder> genericInit) {
			var fieldType = new FieldTypeBuilder();

			genericInit(fieldType);

			Generics.Add((i, fieldType));
		}

		public void SetContraint<T>(string name, T value) =>
			Constraints.Add((name, value));
	}
}
