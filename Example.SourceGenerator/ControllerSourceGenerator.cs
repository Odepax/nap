using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Example.SourceGenerator.Api {
	[Generator]
	public class ControllerSourceGenerator : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) { }
		public void Execute(GeneratorExecutionContext context) {
			var examples = context
				.AdditionalFiles
				.Where(it => Path.GetExtension(it.Path) == ".txt")
				.SelectMany(it => it.GetText().ToString().Split(new[] { "\n\n" }, StringSplitOptions.None));

			var namespacePath = context.Compilation.Assembly.Name;

			foreach (var example in examples) {
				var lines = example.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
				var typeName = lines[0];

				context.AddSource(typeName + "ViewModel.g.cs", $@"
					using System.Collections.Generic;
					using Microsoft.AspNetCore.Mvc;
					using { namespacePath }.Entities;

					namespace { namespacePath }.Controllers {{
						[ApiController]
						[Route(""[controller]"")]
						public partial class { typeName }Controller : ControllerBase {{
							readonly Dictionary<int, { typeName }> Entities = new();

							[HttpGet]
							public ActionResult<IEnumerable<{ typeName }>> List() {{
								return Ok(Entities.Values);
							}}
		
							[HttpGet]
							public ActionResult<{ typeName }> Get(int id) {{
								if (Entities.TryGetValue(id, out var entity))
									return Ok(entity);

								else return NotFound();
							}}
		
							[HttpPut]
							public ActionResult Set({ typeName } entity) {{
								Entities[entity.Id] = entity;

								return NoContent();
							}}
						}}
					}}
				");
			}
		}
	}
}
