using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

namespace Cli
{
	public static class Program
	{
		public static void Main()
		{
			CodeCompileUnit compileUnit = new CodeCompileUnit
			{
				Namespaces =
				{
					new CodeNamespace("Hellow")
					{
						Types =
						{
							new CodeTypeDeclaration("Program")
							{
								Members =
								{
									new CodeEntryPointMethod
									{
										Statements =
										{
											new CodeMethodInvokeExpression(
												new CodeTypeReferenceExpression("System.Console"),
												"WriteLine",
												new CodePrimitiveExpression("Hello World!")
											)
										}
									}
								}
							}
						}
					}
				}
			};

			using TextWriter file = new StreamWriter("Hellow.cs", false);
			using CodeDomProvider code = new CSharpCodeProvider();

			code.GenerateCodeFromCompileUnit(compileUnit, file, new CodeGeneratorOptions
			{
				IndentString = "\t",
				BlankLinesBetweenMembers = true,
				BracingStyle = "C",
				ElseOnClosing = false,
				VerbatimOrder = true
			});
		}
	}
}
