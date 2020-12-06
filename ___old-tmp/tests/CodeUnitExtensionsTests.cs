using System.Threading;
using NUnit.Framework;

namespace Nap.CsGeneration.Tests
{
	[TestFixture]
	public class CodeUnitExtensionsTests
	{
		[Test]
		public void Entity_class_generation()
		{
			Thread.Sleep(1000);
			CodeUnit unit = CodeUnit.New();

			unit.Usings.Add("System");
			unit.Usings.Add("System.Collections.Generic");

			unit.WriteNamespace("GeneratedNapService.Api.Models", @namespace =>
			{
				@namespace.WriteClass("Cat", @class =>
				{
					@class.WriteProperty("Guid", "Id");
					@class.WriteProperty("string", "Name");
					@class.WriteProperty("List<Hooman>", "Slaves");
				});
			});

			string expectedCode = string.Join("\r\n", new[]
			{
				"using System;",
				"using System.Collections.Generic;",
				"",
				"namespace GeneratedNapService.Api.Models",
				"{",
				"	public sealed class Cat",
				"	{",
				"		public Guid Id { get; set; }",
				"		public string Name { get; set; }",
				"		public List<Hooman> Slaves { get; set; }",
				"	}",
				"}",
				""
			});

			Assert.AreEqual(expectedCode, unit.ToString());
		}

		[Test]
		public void Persistency_class_generation()
		{
			Thread.Sleep(1000);
			CodeUnit unit = CodeUnit.New();

			unit.Usings.Add("GeneratedNapService.Api.Models");
			unit.Usings.Add("Microsoft.EntityFrameworkCore");

			unit.WriteNamespace("GeneratedNapService.Api.Data", @namespace =>
			{
				@namespace.WriteClass("GeneratedContext", @base: "DbContext", @class =>
				{
					@class.WriteProperty("DbSet<Cat>", "Cats");

					@class.Code.WriteLineNoTabs(null);

					@class.WriteConstructor(
						"GeneratedContext",
						parameters: new[] { ("DbContextOptions<GeneratedContext>", "options") },
						@base: new[] { "options" },
						implementation =>
						{
						}
					);
				});
			});

			string expectedCode = string.Join("\r\n", new[]
			{
				"using GeneratedNapService.Api.Models;",
				"using Microsoft.EntityFrameworkCore;",
				"",
				"namespace GeneratedNapService.Api.Data",
				"{",
				"	public sealed class GeneratedContext : DbContext",
				"	{",
				"		public DbSet<Cat> Cats { get; set; }",
				"",
				"		public GeneratedContext(DbContextOptions<GeneratedContext> options) : base(options)",
				"		{",
				"		}",
				"	}",
				"}",
				""
			});

			Assert.AreEqual(expectedCode, unit.ToString());
		}

		[Test]
		public void Method_generation()
		{
			Thread.Sleep(1000);
			CodeUnit unit = CodeUnit.New();

			unit.WriteNamespace("GeneratedNapService.Api.Controllers", @namespace =>
			{
				@namespace.WriteClass("CatsController", @class =>
				{
					@class.WriteMethod(
						"ActionResult<Cat>",
						"GetCat",
						parameters: new[] { ("int", "id") },
						implementation =>
						{
						}
					);
				});
			});

			string expectedCode = string.Join("\r\n", new[]
			{
				"namespace GeneratedNapService.Api.Controllers",
				"{",
				"	public sealed class CatsController",
				"	{",
				"		public ActionResult<Cat> GetCat(int id)",
				"		{",
				"		}",
				"	}",
				"}",
				""
			});

			Assert.AreEqual(expectedCode, unit.ToString());
		}
	}
}
