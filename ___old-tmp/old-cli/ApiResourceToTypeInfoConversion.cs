using System;
using System.Linq;
using Nap.CsGeneration;

namespace Nap
{
	public static class ApiResourceToTypeInfoConversion
	{
		public static TypeInfo ToTypeInfo(this ApiResource @this, string apiName) =>
			new TypeInfo
			{
				Namespace = apiName + ".Data",
				Name = @this.Name.ToPascal(),
				Properties = @this.Fields
					.Select(field => field.ToPropertyInfo())
					.Prepend(new PropertyInfo
					{
						Name = "Id",
						Type = new TypeInfo { Namespace = "System", Name = "Guid" }
					})
					.ToList()
			};

		public static PropertyInfo ToPropertyInfo(this ApiResourceField @this) =>
			new PropertyInfo
			{
				Name = @this.Name.ToPascal(),
				Type = @this.Type.ToTypeInfo()
			};

		public static TypeInfo ToTypeInfo(this ApiResourceFieldType @this) =>
			new TypeInfo
			{
				Name = PrimitiveTypeNames.Contains(@this.Name)
					? @this.Name
					: @this.Name.ToPascal()
			};

		private static readonly string[] PrimitiveTypeNames =
		{
			"bool",
			"sbyte",
			"byte",
			"short",
			"ushort",
			"int",
			"uint",
			"long",
			"ulong",
			"char",
			"float",
			"double",
			"decimal",
			"string",
			"object"
		};
	}
}
