using System.Collections.Generic;
using Nap.CsGeneration;

namespace Nap
{
	public static class TypeInfoCodeUnitExtensions
	{
		public static void WriteType(this CodeUnit @this, TypeInfo type)
		{
			@this.WriteNamespace(type.Namespace, @namespace =>
			{
				@namespace.WriteClass(type.Name, @class =>
				{
					foreach (PropertyInfo property in type.Properties)
					{
						if (!(property.Type.Namespace is null))
						{
							@class.Usings.Add(property.Type.Namespace);
						}

						@class.WriteProperty(property.Type.Name, property.Name);
					}
				});
			});
		}

		public static void WriteDbContext(this CodeUnit @this, string apiName, List<TypeInfo> resourceClasses)
		{
			@this.WriteNamespace(apiName + ".Data", @namespace =>
			{
				@namespace.Usings.Add("Microsoft.EntityFrameworkCore");

				@namespace.WriteClass(apiName + "Context", @base: "DbContext", @class =>
				{
					foreach (TypeInfo resource in resourceClasses)
					{
						if (!(resource.Namespace is null))
						{
							@class.Usings.Add(resource.Namespace);
						}

						@class.WriteProperty("DbSet<" + resource.Name + ">", resource.Name.Pluralize());
					}

					@class.Code.WriteLineNoTabs(null);

					@class.WriteConstructor(
						apiName + "Context",
						parameters: new[] { ("DbContextOptions<" + apiName + "Context" + ">", "options") },
						@base: new[] { "options" },
						implementation =>
						{
						}
					);
				});
			});
		}
	}
}
