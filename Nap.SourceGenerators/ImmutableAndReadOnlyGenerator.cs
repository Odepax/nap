using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nap.SourceGenerators {
	[Generator]
	public class ImmutableAndReadOnlyGenerator : ISourceGenerator {
		class SyntaxReceiver : ISyntaxReceiver {
			public readonly List<ClassDeclarationSyntax> CandidateClasses = new List<ClassDeclarationSyntax>();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
				// Any class with attributes is a candidate.
				if (syntaxNode is ClassDeclarationSyntax classDeclaration && 0 < classDeclaration.AttributeLists.Count)
					CandidateClasses.Add(classDeclaration);
			}
		}

		public void Initialize(GeneratorInitializationContext context) {
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		INamedTypeSymbol GenerateImmutableAttribute_symbol;
		INamedTypeSymbol GenerateReadOnlyAttribute_symbol;

		const string GenerateImmutableAttribute_code = @"
			using System;

			namespace Nap.SourceGenerators {
				[AttributeUsage(AttributeTargets.Class, Inherited = false)]
				sealed class GenerateImmutableAttribute : Attribute {}
			}
		";

		const string GenerateReadOnlyAttribute_code = @"
			using System;

			namespace Nap.SourceGenerators {
				[AttributeUsage(AttributeTargets.Class, Inherited = false)]
				sealed class GenerateReadOnlyAttribute : Attribute {}
			}
		";

		public void Execute(GeneratorExecutionContext context) {
			context.AddSource("GenerateImmutableAttribute.cs", GenerateImmutableAttribute_code);
			context.AddSource("GenerateReadOnlyAttribute.cs", GenerateReadOnlyAttribute_code);

			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
				return;

			// Create a new compilation that contains the attribute.
			//
			// TODO:
			// The maintainers wanted to allow source generators to provide source in Initialize,
			// so that this step isn't required.
			// Check out for progress on that.
			var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;

			var compilation = context.Compilation.AddSyntaxTrees(
				CSharpSyntaxTree.ParseText(GenerateImmutableAttribute_code, options),
				CSharpSyntaxTree.ParseText(GenerateReadOnlyAttribute_code, options)
			);

			GenerateImmutableAttribute_symbol = compilation.GetTypeByMetadataName("Nap.SourceGenerators.GenerateImmutableAttribute");
			GenerateReadOnlyAttribute_symbol = compilation.GetTypeByMetadataName("Nap.SourceGenerators.GenerateReadOnlyAttribute");

			// Select the classes annotated with the generation attributes.
			var classSymbols = receiver
				.CandidateClasses

				// Get the symbol being decleared by the class.
				.Select(classDeclaration =>
					compilation
						.GetSemanticModel(classDeclaration.SyntaxTree)
						.GetDeclaredSymbol(classDeclaration)
				)

				// Keep it if annotated.
				.Where(classSymbol =>
					classSymbol.GetAttributes().Any(attrData => AttributeIs(attrData,
						GenerateReadOnlyAttribute_symbol,
						GenerateImmutableAttribute_symbol
					))
				);

			// Generate the source.
			foreach (var classSymbol in classSymbols) {
				string generatedFileName = $"{ classSymbol.Name }_{ nameof(ImmutableAndReadOnlyGenerator) }.cs";
				string code = ProcessClass(context, classSymbol);

				context.AddSource(generatedFileName, code);
			}
		}

		static bool AttributeIs(AttributeData attribute, params INamedTypeSymbol[] oneOf) =>
			oneOf.Contains(attribute.AttributeClass, SymbolEqualityComparer.Default);

		static bool SymbolIs(ISymbol a, ISymbol b) =>
			SymbolEqualityComparer.Default.Equals(a, b);

		string ProcessClass(GeneratorExecutionContext context, INamedTypeSymbol classSymbol) {
			if (!SymbolIs(classSymbol.ContainingSymbol, classSymbol.ContainingNamespace)) {
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
					id: "DPXGEN0001",
					title: "Cannot generate class part.",
					messageFormat: $"Class '{ classSymbol.Name }' must be top-level inside a namespace.",
					category: nameof(ImmutableAndReadOnlyGenerator),
					DiagnosticSeverity.Warning,
					isEnabledByDefault: true
				), Location.None));

				return null;
			}

			var generateImmutable = classSymbol.GetAttributes().Any(attrData => AttributeIs(attrData, GenerateImmutableAttribute_symbol));
			var generateReadOnly = classSymbol.GetAttributes().Any(attrData => AttributeIs(attrData, GenerateReadOnlyAttribute_symbol));

			var visibility = "public";
			var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
			var className = classSymbol.Name;

			var classHumanName = Regex
				.Replace(classSymbol.Name, @"[A-Z]", match => ' ' + match.Value.ToLower())
				.TrimStart();

			var properties = classSymbol
				.GetMembers()
				.Where(member => member.Kind == SymbolKind.Property)
				.Cast<IPropertySymbol>()
				.Where(property => property.GetMethod != null);

			var code = new StringBuilder();

			code.Append("namespace ");
			code.Append(namespaceName);
			code.Append("{");

			code.Append(visibility);
			code.Append(" partial class ");
			code.Append(className);

			if (generateReadOnly) {
				code.Append(":");
				code.Append(className);
				code.Append(".IReadOnly");
			}

			code.Append("{");

			if (generateReadOnly) {
				code.Append("public interface IReadOnly {");

				foreach (var property in properties) {
					var propertyType = property.Type;
					var propertyName = property.Name;

					var readOnlyEquivalenOfPropertyType = propertyType
						.GetAttributes()
						.Any(attrData => AttributeIs(attrData, GenerateReadOnlyAttribute_symbol))
						? propertyType.ToDisplayString() + ".IReadOnly"
						: propertyType.ToDisplayString();

					code.Append("public ");
					code.Append(readOnlyEquivalenOfPropertyType);
					code.Append(" ");
					code.Append(propertyName);
					code.Append("{ get; }");
				}

				code.Append("}");
			}


			if (generateImmutable) {
				code.Append("public class Immutable");

				if (generateReadOnly) {
					code.Append(":");
					code.Append(className);
					code.Append(".IReadOnly");
				}

				code.Append("{");

				foreach (var property in properties) {
					var propertyType = property.Type;
					var propertyName = property.Name;

					var immutableEquivalenOfPropertyType = propertyType
						.GetAttributes()
						.Any(attrData => AttributeIs(attrData, GenerateReadOnlyAttribute_symbol))
						? propertyType.ToDisplayString() + ".Immutable"
						: propertyType.ToDisplayString();

					code.Append("public ");
					code.Append(immutableEquivalenOfPropertyType);
					code.Append(" ");
					code.Append(propertyName);
					code.Append("{ get; init; }");
				}

				code.Append("}");
			}

			code.Append("}");

			if (generateReadOnly) {
				code.Append("public static class AsReadOnly");
				code.Append(className);
				code.Append("Extensions {");

				code.Append("\n/// <summary>Returns a read-only version of this ");
				code.Append(classHumanName);
				code.Append(".</summary>");
				code.Append("\n/// <returns>The same object, typed as read-only.</returns>");
				code.Append("\npublic static ");
				code.Append(className);
				code.Append(".IReadOnly AsReadOnly(this ");
				code.Append(className);
				code.Append(".IReadOnly @this) => @this;");

				code.Append("}");
			}

			if (generateImmutable) {
				code.Append("public static class ToImmutable");
				code.Append(className);
				code.Append("Extensions {");

				code.Append("\n/// <summary>Creates and returns an immutable copy of this ");
				code.Append(classHumanName);
				code.Append(".</summary>");
				code.Append("\n/// <returns>A new object.</returns>");
				code.Append("\npublic static ");
				code.Append(className);
				code.Append(".Immutable ToImmutable(this ");
				code.Append(className);

				if (generateReadOnly)
					code.Append(".IReadOnly");

				code.Append(" @this) => new ");
				code.Append(className);
				code.Append(".Immutable {");

				foreach (var property in properties) {
					var propertyName = property.Name;

					code.Append(propertyName);
					code.Append("= @this.");
					code.Append(propertyName);
					code.Append(",");
				}

				code.Append("};");
				code.Append("}");
			}

			/* If... Of course we generate mutable... */ {
				code.Append("public static class ToMutable");
				code.Append(className);
				code.Append("Extensions {");

				code.Append("\n/// <summary>Creates and returns a mutable copy of this ");
				code.Append(classHumanName);
				code.Append(".</summary>");
				code.Append("\n/// <returns>A new object.</returns>");
				code.Append("\npublic static ");
				code.Append(className);
				code.Append(" ToMutable(this ");
				code.Append(className);

				if (generateReadOnly)
					code.Append(".IReadOnly");
				else
					code.Append(".Immutable");

				code.Append(" @this) => new ");
				code.Append(className);
				code.Append("{");

				foreach (var property in properties) {
					var propertyName = property.Name;

					code.Append(propertyName);
					code.Append("= @this.");
					code.Append(propertyName);
					code.Append(",");
				}

				code.Append("};");
				code.Append("}");
			}

			code.Append("}");

			return code.ToString();
		}

		// Below looks like old shit...

		void ProcessField(StringBuilder code, IFieldSymbol fieldSymbol, ISymbol AutoNotifyAttribute_symbol) {
			var fieldName = fieldSymbol.Name;
			var fieldType = fieldSymbol.Type;

			var overridenPropertyName = fieldSymbol
				.GetAttributes()
				.Single(attrData => attrData.AttributeClass.Equals(AutoNotifyAttribute_symbol, SymbolEqualityComparer.Default))
				.NamedArguments
				.SingleOrDefault(kvp => kvp.Key == "PropertyName")
				.Value
				.Value as string;

			var propertyName = overridenPropertyName ?? (
				fieldName.Length == 1
					? fieldName.ToUpper()
					: fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1)
			);

			if (string.IsNullOrWhiteSpace(propertyName)) {
				//TODO: issue a diagnostic that we can't process this field
				return;
			}

			code.Append($@"
public { fieldType } { propertyName } {{
get {{
	return this.{ fieldName };
}}
set {{
	this.{ fieldName } = value;
	this.PropertyChanged?.Invoke(this, PropertyChangedEventArgs_{ propertyName });
}}
}}
static readonly System.ComponentModel.PropertyChangedEventArgs PropertyChangedEventArgs_{ propertyName } = new System.ComponentModel.PropertyChangedEventArgs(nameof({ propertyName }));
");
		}
	}
}
