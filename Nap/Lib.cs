using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// TODO: Docstrings!
// TODO: Make ContextBuilder.set_Name and ContextBuilder.AddContainer() thread-safe.

namespace Nap {
	/// <summary>
	/// ...
	/// </summary>
	public interface IInputMethod {
		Task PopulateAsync(IContextBuilder context);
	}

	/// <summary>
	/// ...
	/// </summary>
	public interface IOutputMethod {
		Task GenerateAsync(Context context);
	}

	public static class Nap {
		/// <summary>
		/// ...
		/// </summary>
		///
		/// <returns>
		/// ...
		/// </returns>
		public static async Task GenerateAsync(IEnumerable<IInputMethod> inputs, IEnumerable<IOutputMethod> outputs) {
			var context = new ContextBuilder();
			await Task.WhenAll(inputs.Select(input => input.PopulateAsync(context))).ConfigureAwait(false);

			var mergedContext = context.Build();
			await Task.WhenAll(outputs.Select(output => output.GenerateAsync(mergedContext))).ConfigureAwait(false);
		}
	}

	public class NapContextException : Exception {
		internal NapContextException(string message, Exception? innerException = default) : base(message, innerException) {}
	}

	// Inputs
	// ----

	/// <see cref="Context"/>
	public interface IContextBuilder {
		string Name { set; }
		void AddContainer(Action<IContainerBuilder> init);
		Context Build();
	}

	/// <see cref="Container"/>
	public interface IContainerBuilder {
		string Name { set; }
		void AddResource(Action<IResourceTypeBuilder> init);
	}

	/// <see cref="ResourceType"/>
	public interface IResourceTypeBuilder {
		string Name { set; }

		/// <summary>
		/// Sets the name of the <paramref name="i"/>th generic type parameter of this type.
		/// </summary>
		///
		/// <param name="i">
		/// The 0-based index of the generic.
		/// </param>
		void SetGenericTemplate(int i, string genericName);

		void SetFieldType(string fieldName, Action<IFieldTypeBuilder> typeInit);
	}

	/// <see cref="DataType"/>
	public interface IFieldTypeBuilder {
		string Name { set; }

		/// <summary>
		/// Assigns a type to the <paramref name="i"/>th generic type parameter of this type.
		/// </summary>
		///
		/// <param name="i">
		/// The 0-based index of the generic.
		/// </param>
		void SetGeneric(int i, Action<IFieldTypeBuilder> genericInit);

		void SetContraint<T>(string name, T value);
	}

	// Outputs
	// ----
	// NO! You can't just replace regions with comments and decorations!
	// Dashes go: Brrr...

	/// <summary>
	/// ...
	/// </summary>
	public sealed class Context {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<Container> Containers { get; init; } = ImmutableList<Container>.Empty;
	}

	/// <summary>
	/// ...
	/// </summary>
	public sealed class Container {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<Resource> Resources { get; init; } = ImmutableList<Resource>.Empty;
	}

	/// <summary>
	/// ...
	/// </summary>
	public sealed class Resource {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<TemplateType> GenericTemplates { get; init; } = ImmutableList<TemplateType>.Empty;
		public IReadOnlyDictionary<string, DataType> Fields { get; init; } = new Dictionary<string, DataType>(0);
	}

	/// <summary>
	/// ...
	/// </summary>
	public abstract class DataType {
		private protected DataType() {}

		public string? SameAs { get; init; }
		public string? NotSameAs { get; init; }
	}

	/// <summary>
	/// ...
	/// </summary>
	public sealed class TemplateType : DataType {
		public string Name { get; init; } = string.Empty;

		public bool IsOptional { get; init; } = false;
	}

	/// <summary>
	/// ...
	/// </summary>
	public sealed class ResourceType : DataType {
		public string Name { get; init; } = string.Empty;
		public IReadOnlyList<DataType> Generics { get; init; } = ImmutableList<DataType>.Empty;

		public SelfReference SelfReference { get; init; } = SelfReference.Allow;
		public bool IsOptional { get; init; } = false;
	}

	public enum SelfReference {
		/// <summary> Tolerate references to all objects. </summary>
		Allow,

		/// <summary> Must reference the containing object. </summary>
		Enforce,

		/// <summary> Must not reference the containing object. </summary>
		Forbid
	}

	public sealed class BoolType : DataType {
		public bool? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class IntType : DataType {
		public IReadOnlyCollection<int> AllowedValues { get; init; } = ImmutableList<int>.Empty;
		public IReadOnlyCollection<int> ForbiddenValues { get; init; } = ImmutableList<int>.Empty;

		public int? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public int? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public int? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public int? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public int? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class FloatType : DataType {
		public IReadOnlyCollection<float> AllowedValues { get; init; } = ImmutableList<float>.Empty;
		public IReadOnlyCollection<float> ForbiddenValues { get; init; } = ImmutableList<float>.Empty;

		public float? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public float? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public float? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public float? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public float? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class CharType : DataType {
		public IReadOnlyCollection<char> AllowedValues { get; init; } = ImmutableList<char>.Empty;
		public IReadOnlyCollection<char> ForbiddenValues { get; init; } = ImmutableList<char>.Empty;

		public char? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class StringType : DataType {
		public IReadOnlyCollection<string> AllowedValues { get; init; } = ImmutableList<string>.Empty;
		public IReadOnlyCollection<string> ForbiddenValues { get; init; } = ImmutableList<string>.Empty;
		public Regex? Pattern { get; init; }

		public bool AllowsEmpty { get; init; } = true;
		public bool AllowsMultiline { get; init; } = false;

		public int? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public int? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public int? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public int? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public string? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class DateType : DataType {
		public IReadOnlyCollection<DateTime> AllowedValues { get; init; } = ImmutableList<DateTime>.Empty;
		public IReadOnlyCollection<DateTime> ForbiddenValues { get; init; } = ImmutableList<DateTime>.Empty;

		public DateTime? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public DateTime? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public DateTime? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public DateTime? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public DateTime? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class DatetimeType : DataType {
		public IReadOnlyCollection<DateTime> AllowedValues { get; init; } = ImmutableList<DateTime>.Empty;
		public IReadOnlyCollection<DateTime> ForbiddenValues { get; init; } = ImmutableList<DateTime>.Empty;

		public DateTime? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public DateTime? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public DateTime? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public DateTime? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public DateTime? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class DurationType : DataType {
		public IReadOnlyCollection<TimeSpan> AllowedValues { get; init; } = ImmutableList<TimeSpan>.Empty;
		public IReadOnlyCollection<TimeSpan> ForbiddenValues { get; init; } = ImmutableList<TimeSpan>.Empty;

		public TimeSpan? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public TimeSpan? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public TimeSpan? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public TimeSpan? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;

		public TimeSpan? Default { get; init; }
		public bool IsOptional { get; init; } = false;
	}

	public sealed class SetType : DataType {
		public DataType ElementType { get; }

		public SetType(DataType elementType) => ElementType = elementType;

		public bool AllowsDuplicates { get; init; } = true;
		public bool AllowsEmpty { get; init; } = true;

		public int? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public int? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public int? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public int? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;
	}

	public sealed class ListType : DataType {
		public DataType ElementType { get; }

		public ListType(DataType elementType) => ElementType = elementType;

		public bool AllowsDuplicates { get; init; } = true;
		public bool AllowsEmpty { get; init; } = true;

		public int? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public int? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public int? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public int? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;
	}

	public sealed class MapType : DataType {
		public DataType KeyType { get; }
		public DataType ElementType { get; }

		public MapType(DataType keyType, DataType elementType) {
			KeyType = keyType;
			ElementType = elementType;
		}

		public bool AllowsDuplicates { get; init; } = true;
		public bool AllowsEmpty { get; init; } = true;

		public int? Min { get; init; }
		public bool MinIsInclusive { get; init; } = true;
		public int? Max { get; init; }
		public bool MaxIsInclusive { get; init; } = true;

		public int? ExclusionMin { get; init; }
		public bool ExclusionMinIsInclusive { get; init; } = true;
		public int? ExclusionMax { get; init; }
		public bool ExclusionMaxIsInclusive { get; init; } = true;
	}
}
