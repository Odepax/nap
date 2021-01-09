using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Example.SourceGenerator.Api {
	[Generator]
	public class EntitySourceGenerator : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) { }
		public void Execute(GeneratorExecutionContext context) {
			var examples = context
				.AdditionalFiles
				.Where(it => Path.GetExtension(it.Path) == ".txt")
				.SelectMany(it => it.GetText().ToString().Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.None));

			var namespacePath = context.Compilation.Assembly.Name + ".Entities";

			foreach (var example in examples) {
				var lines = example.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
				var typeName = lines[0];

				context.AddSource(typeName + ".g.cs", $@"
					namespace { namespacePath } {{
						public partial class { typeName } {{
							{ (string.Join("\n", lines
								.Skip(1)
								.Select(line => line.Split(' '))
								.Select(it => (Type: it[1], Name: it[0]))
								.Select(property => $@"public { property.Type } { property.Name } {{ get; set; }}")
							)) }
						}}
					}}
				");
			}
		}
	}
}
