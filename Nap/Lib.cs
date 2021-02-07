using System;
using System.Collections.Generic;
using System.Collections.Immutable;

// TODO: Re-type Resource.GenericTemplates to strings?

namespace Nap {
	public class NapContextMergingException : Exception {
		internal NapContextMergingException(string message) : base(message) {}
	}

	public static class NapBuiltInTypes {
		public const string Bool = "bool";
		public const string Int = "int";
		public const string Float = "float";
		public const string Char = "char";
		public const string String = "string";
		public const string Date = "date";
		public const string Datetime = "datetime";
		public const string Duration = "duration";
		public const string Set = "set";
		public const string List = "list";
		public const string Map = "map";
	}

	public static class NapBuiltInMeta {
		public const string Min = "min";
		public const string Max = "max";
		public const string MinIsInclusive = "min inclusive";
		public const string MaxIsInclusive = "max inclusive";
		public const string AllowedValues = "allowed values";
		public const string ForbiddenValues = "forbidden values";
		public const string AllowEmpty = "empty";
		public const string AllowDuplicates = "duplicates";
		public const string AllowMultiline = "multiline";
		public const string Pattern = "pattern";
		public const string Default = "default";
		public const string IsOptional = "optional";
		public const string SameAs = "same as";
		public const string NotSameAs = "not same as";
		public const string SelfReference = "self";
	}

	public enum SelfReference {
		/// <summary> Tolerate references to all objects. </summary>
		Allow,

		/// <summary> Must reference the containing object. </summary>
		Enforce,

		/// <summary> Must not reference the containing object. </summary>
		Forbid
	}

	// Inputs
	// ----

	/// <summary>
	/// Represents a partial, in-construction Nap <see cref="Context"/>.
	///
	/// The faith of all partial context is to be merged with other partial contexts,
	/// in order to produce a final context.
	/// </summary>
	public sealed class PartialContext {
		/// <summary>
		/// Merges one or more partial contexts into a final Nap context.
		/// </summary>
		///
		/// <exception cref="NapContextMergingException">
		/// Thrown when the <paramref name="partials"/> don't merge well together.
		/// </exception>
		public static Context Merge(IReadOnlyCollection<PartialContext> partials) =>
			new MergingContext().MergeContexts(partials);

		/// <summary>
		/// When merging, at least one partial context must have a non-null, non-blank name.
		/// All such named partial contexts must have the same name.
		///
		/// If you're looking for conventions: use lower case, space-separated names.
		///
		/// Leave null or blank not to participate in the merged context's naming.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Containers with the same name will be grouped and merged together,
		/// so it's perfectly valid to add two complementary containers with the same name
		/// inside the same context.
		/// </summary>
		public ICollection<PartialContainer> Containers { get; } = new List<PartialContainer>();
	}

	/// <summary>
	/// Represents a partial, in-construction <see cref="Container"/>.
	/// </summary>
	public sealed class PartialContainer {
		/// <summary>
		/// Must be set: merging null- or blank-named partial containers will throw.
		///
		/// If you're looking for conventions: use lower case, space-separated names.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Within a group of similarly-named partial containers,
		/// partial resources with the same name will be grouped and merged together.
		///
		/// It is perfectly valid to add two complementary resources with the same name
		/// inside this container.
		/// </summary>
		public ICollection<PartialResource> Resources { get; } = new List<PartialResource>();
	}

	/// <summary>
	/// Represents a partial, in-construction <see cref="Resource"/>.
	/// </summary>
	public sealed class PartialResource {
		/// <summary>
		/// Must be set: merging null- or blank-named partial resources will throw.
		/// Keep in mind that it is invalid to override <see cref="NapBuiltInTypes"/>.
		///
		/// If you're looking for conventions: use lower case, space-separated names.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// The key is the positive 0-based index of the generic template type.
		/// Holes in the index sequence are acceptable,
		/// as long as complementary resources fill them at merge time.
		///
		/// The value is the non-blank name of the generic template type.
		/// If you're looking for conventions: use lower case, space-separated names.
		/// </summary>
		public IDictionary<int, string> GenericTemplates { get; } = new Dictionary<int, string>();

		/// <summary>
		/// The key is the non-blank name of the field.
		/// If you're looking for conventions: use lower case, space-separated names.
		///
		/// The value is the type of the field.
		/// </summary>
		public IDictionary<string, PartialFieldType> Fields { get; } = new Dictionary<string, PartialFieldType>();
	}

	/// <summary>
	/// Represents a partial, in-construction <see cref="FieldType"/>.
	/// </summary>
	public sealed class PartialFieldType {
		/// <summary>
		/// When merging, at least one partial field type must have a non-null, non-blank name.
		/// All such named partial fields must have the same name.
		///
		/// Use the constants defined in <see cref="NapBuiltInTypes"/> to mitigate typing mistakes
		/// when referencing built-in primitive types.
		///
		/// If you're looking for conventions: use lower case, space-separated names.
		///
		/// Leave null or blank not to participate in the merged field type's naming.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// The key is the positive 0-based index of the generic type.
		/// Holes in the index sequence are acceptable,
		/// as long as complementary field types fill them at merge time.
		///
		/// The value is the generic type itself.
		/// </summary>
		public IDictionary<int, PartialFieldType> Generics { get; } = new Dictionary<int, PartialFieldType>();

		/// /// <summary>
		/// Free-For-All metadata bag.
		/// Use <see cref="NapBuiltInMeta"/> keys to set built-in Nap meta.
		///
		/// If you're looking for conventions: use lower case, space-separated names,
		/// use <c>.</c> to namespace your custom meta names.
		/// </summary>
		public IDictionary<string, object?> Meta { get; } = new Dictionary<string, object?>();
	}

	// Outputs
	// ----

	/// <summary>
	/// Inspired by <see href="https://c4model.com"/>, a context represents an entire software system.
	/// </summary>
	public sealed class Context {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyCollection<Container> Containers { get; init; } = ImmutableList<Container>.Empty;
	}

	/// <summary>
	/// Inspired by <see href="https://c4model.com"/>, a container represents anything like
	/// a web-API is a container, so is a database, a web front-end, or a mobile app.
	///
	/// A Docker container isn't really a C4 container:
	/// the C4 container would be the service that runs inside the Docker container.
	/// </summary>
	public sealed class Container {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyCollection<Resource> Resources { get; init; } = ImmutableList<Resource>.Empty;
	}

	/// <summary>
	/// A Nap resource represents a data structure; see it as some kind of JSON Schema, or a DTO class.
	/// </summary>
	public sealed class Resource {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<TemplateType> GenericTemplates { get; init; } = ImmutableList<TemplateType>.Empty;
		public IReadOnlyDictionary<string, FieldType> Fields { get; init; } = ImmutableDictionary<string, FieldType>.Empty;
	}

	/// <summary>
	/// Base class for built-in field types. Sub-classes represent the actual field types,
	/// in a way similar to Java's enumerations, Kotlin's sealed classes, or Swift's value enums.
	/// </summary>
	public abstract class FieldType {
		private protected FieldType() {}

		/// <summary>
		/// Metadata bag containing <see cref="NapBuiltInMeta"/> and, potentially, custom meta.
		/// </summary>
		public IReadOnlyDictionary<string, object?> Meta { get; init; } = ImmutableDictionary<string, object?>.Empty;
	}

	/// <summary>
	/// This field type represents a reference to a generic template declared in the containing resource.
	/// </summary>
	///
	/// <seealso cref="Resource.GenericTemplates"/>
	public sealed class TemplateType : FieldType {
		public string Name { get; init; } = string.Empty;
	}

	/// <summary>
	/// This field type represents a reference to another resource, which may not be declared.
	/// </summary>
	public sealed class ResourceType : FieldType {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<FieldType> Generics { get; init; } = ImmutableList<FieldType>.Empty;
	}

	public sealed class BoolType : FieldType {}

	public sealed class IntType : FieldType {}
	public sealed class FloatType : FieldType {}

	public sealed class CharType : FieldType {}
	public sealed class StringType : FieldType {}

	public sealed class DateType : FieldType {}
	public sealed class DatetimeType : FieldType {}
	public sealed class DurationType : FieldType {}

	public sealed class SetType : FieldType {
		public FieldType ElementType { get; }

		public SetType(FieldType elementType) => ElementType = elementType;
	}

	public sealed class ListType : FieldType {
		public FieldType ElementType { get; }

		public ListType(FieldType elementType) => ElementType = elementType;
	}

	public sealed class MapType : FieldType {
		public FieldType KeyType { get; }
		public FieldType ElementType { get; }

		public MapType(FieldType keyType, FieldType elementType) {
			KeyType = keyType;
			ElementType = elementType;
		}
	}
}
