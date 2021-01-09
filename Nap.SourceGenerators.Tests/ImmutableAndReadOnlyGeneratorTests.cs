using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Nap.SourceGenerators.Tests {
	static class ImmutableAndReadOnlyGeneratorTests {
		#pragma warning disable CS8618 // No need to freak out about nullability, the OneTimeSetUp will set it...
		static ImmutableAndReadOnlyGenerator Generator;
		#pragma warning restore CS8618

		[OneTimeSetUp]
		public static void SetUp() {
			Generator = new ImmutableAndReadOnlyGenerator();
		}

		[Test]
		public static void It_doesnt_generate_if_the_input_is_empty() {
			var source = string.Empty;
			var expectedGeneration = string.Empty;

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_doesnt_generate_if_the_input_doesnt_contain_marked_classes() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					public class Cat {
						public int Id { get; set; }
						public string Name { get; set; }
					}
				}
			";

			var expectedGeneration = string.Empty;

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_generates_an_empty_immutable_class() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					[GenerateImmutable]
					public partial class Cat {
					}
				}
			";

			var expectedGeneration = @"
				namespace Youpi {
					public partial class Cat {
						public class Immutable {}
					}

					public static class ToImmutableCatExtensions {
						/// <summary>Creates and returns an immutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat.Immutable ToImmutable(this Cat @this) => new Cat.Immutable {};
					}

					public static class ToMutableCatExtensions {
						/// <summary>Creates and returns a mutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat ToMutable(this Cat.Immutable @this) => new Cat {};
					}
				}
			";

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_generates_a_simple_immutable_class() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					[GenerateImmutable]
					public partial class Cat {
						public int Id { get; set; }
						public string Name { get; set; }
					}
				}
			";

			var expectedGeneration = @"
				namespace Youpi {
					public partial class Cat {
						public class Immutable {
							public int Id { get; init; }
							public string Name { get; init; }
						}
					}

					public static class ToImmutableCatExtensions {
						/// <summary>Creates and returns an immutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat.Immutable ToImmutable(this Cat @this) => new Cat.Immutable {
							Id = @this.Id,
							Name = @this.Name,
						};
					}

					public static class ToMutableCatExtensions {
						/// <summary>Creates and returns a mutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat ToMutable(this Cat.Immutable @this) => new Cat {
							Id = @this.Id,
							Name = @this.Name,
						};
					}
				}
			";

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_generates_an_empty_readonly_class() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					[GenerateReadOnly]
					public partial class Cat {
					}
				}
			";

			var expectedGeneration = @"
				namespace Youpi {
					public partial class Cat : Cat.IReadOnly {
						public interface IReadOnly {}
					}

					public static class AsReadOnlyCatExtensions {
						/// <summary>Returns a read-only version of this cat.</summary>
						/// <returns>The same object, typed as read-only.</returns>
						public static Cat.IReadOnly AsReadOnly(this Cat.IReadOnly @this) => @this;
					}

					public static class ToMutableCatExtensions {
						/// <summary>Creates and returns a mutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat ToMutable(this Cat.IReadOnly @this) => new Cat {};
					}
				}
			";

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_generates_a_simple_readonly_class() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					[GenerateReadOnly]
					public partial class Cat {
						public int Id { get; set; }
						public string Name { get; set; }
					}
				}
			";

			var expectedGeneration = @"
				namespace Youpi {
					public partial class Cat : Cat.IReadOnly {
						public interface IReadOnly {
							public int Id { get; }
							public string Name { get; }
						}
					}

					public static class AsReadOnlyCatExtensions {
						/// <summary>Returns a read-only version of this cat.</summary>
						/// <returns>The same object, typed as read-only.</returns>
						public static Cat.IReadOnly AsReadOnly(this Cat.IReadOnly @this) => @this;
					}

					public static class ToMutableCatExtensions {
						/// <summary>Creates and returns a mutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat ToMutable(this Cat.IReadOnly @this) => new Cat {
							Id = @this.Id,
							Name = @this.Name,
						};
					}
				}
			";

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}

		[Test]
		public static void It_generates_simple_immutable_and_readonly_classes() {
			var source = @"
				using Nap.SourceGenerators;

				namespace Youpi {
					[GenerateImmutable]
					[GenerateReadOnly]
					public partial class Cat {
						public int Id { get; set; }
						public string Name { get; set; }
					}
				}
			";

			var expectedGeneration = @"
				namespace Youpi {
					public partial class Cat : Cat.IReadOnly {
						public interface IReadOnly {
							public int Id { get; }
							public string Name { get; }
						}

						public class Immutable : Cat.IReadOnly {
							public int Id { get; init; }
							public string Name { get; init; }
						}
					}

					public static class AsReadOnlyCatExtensions {
						/// <summary>Returns a read-only version of this cat.</summary>
						/// <returns>The same object, typed as read-only.</returns>
						public static Cat.IReadOnly AsReadOnly(this Cat.IReadOnly @this) => @this;
					}

					public static class ToImmutableCatExtensions {
						/// <summary>Creates and returns an immutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat.Immutable ToImmutable(this Cat.IReadOnly @this) => new Cat.Immutable {
							Id = @this.Id,
							Name = @this.Name,
						};
					}

					public static class ToMutableCatExtensions {
						/// <summary>Creates and returns a mutable copy of this cat.</summary>
						/// <returns>A new object.</returns>
						public static Cat ToMutable(this Cat.IReadOnly @this) => new Cat {
							Id = @this.Id,
							Name = @this.Name,
						};
					}
				}
			";

			var (actualGenerations, diagnostics) = GetGeneratorOutput(source);

			AssertGeneration(expectedGeneration, actualGenerations);
			Assert.IsEmpty(diagnostics);
		}


		/*

		it fails if the marked class is not partial
		it fails if the marked class is not toplevel
		it generates internal marked classes
		it generates immutable properties with default values
		lists
		sets
		maps
		custom types marked with readonly
		custom types marked with immutable
		custom types marked with readonly and immutable
		custom types not marked
		get-only properties
		set-only properties

		*/

		static (List<string> Generations, ImmutableArray<Diagnostic> Diagnostics) GetGeneratorOutput(string inputSource) {
			var syntaxTree = CSharpSyntaxTree.ParseText(inputSource);

			var references = AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(assembly => !assembly.IsDynamic && !assembly.FullName.StartsWith("Mono.Cecil"))
				.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
				.ToList();

			var compilation = CSharpCompilation.Create("test", new SyntaxTree[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			CSharpGeneratorDriver
				.Create(Generator)
				.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatedDiagnostics);

			// SyntaxTrees[0] is the input.
			// SyntaxTrees[1] and [2] are the attributes.
			var generatedSources = outputCompilation.SyntaxTrees
				.Skip(3)
				.Select(it => Regex.Replace(it.ToString(), @"\s+", string.Empty))
				.ToList();

			return (generatedSources, generatedDiagnostics);
		}

		static void AssertGeneration(string? expectedGeneration, List<string> actualGenerations) {
			if (expectedGeneration == null || expectedGeneration == string.Empty)
				Assert.IsEmpty(actualGenerations);

			else {
				expectedGeneration = Regex.Replace(expectedGeneration, @"\s+", string.Empty);

				Assert.Contains(expectedGeneration, actualGenerations);
			}
		}
	}
}
