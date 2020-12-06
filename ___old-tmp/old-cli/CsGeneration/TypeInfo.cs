using System.Collections.Generic;

namespace Nap.CsGeneration
{
	public sealed class TypeInfo
	{
		public string? Namespace { get; set; }
		public string? Name { get; set; }
		public TypeInfo? Base { get; set; }
		public List<TypeInfo> TypeParameters { get; set; } = new List<TypeInfo>(0);
		public List<AttributeInfo> Attributes { get; set; } = new List<AttributeInfo>(0);
		public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>(0);
		public List<ConstructorInfo> Constructors { get; set; } = new List<ConstructorInfo>(0);
	}

	public sealed class AttributeInfo
	{
		public TypeInfo? Type { get; set; }
		public List<string> Parameters { get; set; } = new List<string>(0);
	}

	public sealed class PropertyInfo
	{
		public string? Name { get; set; }
		public TypeInfo? Type { get; set; }
		public List<AttributeInfo> Attributes { get; set; } = new List<AttributeInfo>(0);
	}

	public sealed class ConstructorInfo
	{
		public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>(0);
		public List<string> Base { get; set; } = new List<string>(0);
	}

	public sealed class ParameterInfo
	{
		public string? Name { get; set; }
		public TypeInfo? Type { get; set; }
		public List<AttributeInfo> Attributes { get; set; } = new List<AttributeInfo>(0);
	}
}
