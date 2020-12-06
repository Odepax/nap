using System.Collections.Generic;

namespace Nap
{
	public sealed class ApiResource
	{
		public string Name { get; }
		public IReadOnlyList<ApiResourceField> Fields { get; }
		public IReadOnlyList<ApiResourceAnnotation> Annotations { get; }

		public ApiResource(string name, IEnumerable<ApiResourceField> fields, IEnumerable<ApiResourceAnnotation> annotations)
		{
			Name = name;
			Fields = new List<ApiResourceField>(fields);
			Annotations = new List<ApiResourceAnnotation>(annotations);
		}
	}

	public sealed class ApiResourceField
	{
		public string Name { get; }
		public ApiResourceFieldType Type { get; }
		public IReadOnlyList<ApiResourceAnnotation> Annotations { get; }

		public ApiResourceField(string name, ApiResourceFieldType type, IEnumerable<ApiResourceAnnotation> annotations)
		{
			Name = name;
			Type = type;
			Annotations = new List<ApiResourceAnnotation>(annotations);
		}
	}

	public sealed class ApiResourceFieldType
	{
		public string Name { get; }
		public IReadOnlyList<ApiResourceFieldType> Generics { get; }

		public ApiResourceFieldType(string name, IEnumerable<ApiResourceFieldType> generics)
		{
			Name = name;
			Generics = new List<ApiResourceFieldType>(generics);
		}
	}

	public sealed class ApiResourceAnnotation
	{
		public string Name { get; }
		public IReadOnlyList<object> Arguments { get; }

		public ApiResourceAnnotation(string name, IEnumerable<object> arguments)
		{
			Name = name;
			Arguments = new List<object>(arguments);
		}
	}
}
