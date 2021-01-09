using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Example.SourceGenerator.Wpf {
	[Generator]
	public class ViewModelSourceGenerator : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) { }
		public void Execute(GeneratorExecutionContext context) {
			var examples = context
				.AdditionalFiles
				.Where(it => Path.GetExtension(it.Path) == ".txt")
				.SelectMany(it => it.GetText().ToString().Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.None));

			var namespacePath = context.Compilation.Assembly.Name + ".ViewModels";

			foreach (var example in examples) {
				var lines = example.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
				var typeName = lines[0];

				context.AddSource(typeName + "ViewModel.g.cs", $@"
					using System.ComponentModel;
					using System.Runtime.CompilerServices;

					namespace { namespacePath } {{
						public partial class { typeName }ViewModel : INotifyPropertyChanged {{
							public event PropertyChangedEventHandler PropertyChanged;

							void FirePropertyChanged([CallerMemberName] string propertyName = null) =>
								PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

							{ (string.Join("\n", lines
								.Skip(1)
								.Select(line => line.Split(' '))
								.Select(it => (Type: it[1], Name: it[0]))
								.Select(property => $@"
									{ property.Type } { property.Name }_field;
									public { property.Type } { property.Name } {{
										get => { property.Name }_field;
										set {{
											if (!value.Equals({ property.Name }_field)) {{
												{ property.Name }_field = value;
												FirePropertyChanged();
											}}
										}}
									}}
								")
							)) }
						}}
					}}
				");
			}
		}
	}
}
