using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Nap.Tests {
	static class ContextMergingTests {
		[Test]
		[TestCaseSource(nameof(TestCases))]
		public static void Tests(IReadOnlyCollection<PartialContext> contexts, object expected) {
			if (expected is Context expectedBuildResult) {
				var actualBuildResult = PartialContext.Merge(contexts);

				AssertContext(expectedBuildResult, actualBuildResult);
			}

			else if (expected is string expectedExceptionMessage) {
				var actualException = Assert.Throws<NapContextMergingException>(() => PartialContext.Merge(contexts));

				Assert.AreEqual(expectedExceptionMessage, actualException.Message);
			}

			else throw new Exception("9E4B5BEC-6088-45DF-845B-00C1A0915AD0: Wrong parametrized test expectation.");
		}

		static TestCaseData TestCase(string name, IReadOnlyCollection<PartialContext> contexts, Context expected) =>
			new TestCaseData(contexts, expected).SetName(name);

		static TestCaseData TestCase(string name, IReadOnlyCollection<PartialContext> contexts, string expectedExceptionMessage) =>
			new TestCaseData(contexts, expectedExceptionMessage).SetName(name);

		static TestCaseData TestCase(string name, PartialContext context, Context expected) =>
			new TestCaseData(new[]{ context }, expected).SetName(name);

		static TestCaseData TestCase(string name, PartialContext context, string expectedExceptionMessage) =>
			new TestCaseData(new[]{ context }, expectedExceptionMessage).SetName(name);

		static readonly IEnumerable<object> TestCases = new[] {
			// Contexts
			// ----

			TestCase("Add context name",
				new PartialContext { Name = "test" },
				new Context { Name = "test" }
			),

			TestCase("No context name",
				new PartialContext(),
				"Unnamed context."
			),

			TestCase("Different context names",
				new[] {
					new PartialContext { Name = "a" },
					new PartialContext { Name = "b" }
				},
				"Different context names."
			),

			// Containers
			// ----

			TestCase("Add container",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer { Name = "test" }
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container { Name = "test" }
					}
				}
			),

			TestCase("Add containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer { Name = "a" },
						new PartialContainer { Name = "b" }
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container { Name = "a" },
						new Container { Name = "b" }
					}
				}
			),

			TestCase("Add container twice",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer { Name = "test" },
						new PartialContainer { Name = "test" }
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container { Name = "test" }
					}
				}
			),

			TestCase("No container name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer()
					}
				},
				"Unnamed container."
			),

			// Resources
			// ----

			TestCase("Add resource",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource { Name = "test" }
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource { Name = "test" }
							}
						}
					}
				}
			),

			TestCase("No resource name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource()
							}
						}
					}
				},
				"Unnamed resource in container 'test'."
			),

			TestCase("Add resources",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource { Name = "a" },
								new PartialResource { Name = "b" }
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource { Name = "a" },
								new Resource { Name = "b" }
							}
						}
					}
				}
			),

			TestCase("Add resource twice",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource { Name = "test" },
								new PartialResource { Name = "test" }
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource { Name = "test" }
							}
						}
					}
				}
			),

			TestCase("Add resources across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource { Name = "a" },
									new PartialResource { Name = "b" }
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource { Name = "c" }
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource { Name = "d" }
								}
							},
							new PartialContainer {
								Name = "other",
								Resources = {
									new PartialResource { Name = "e" }
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource { Name = "a" },
								new Resource { Name = "b" },
								new Resource { Name = "c" },
								new Resource { Name = "d" }
							}
						},
						new Container {
							Name = "other",
							Resources = new[] {
								new Resource { Name = "e" }
							}
						}
					}
				}
			),

			// Generic Templates
			// ----

			TestCase("Add generic template",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "test"
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									GenericTemplates = new[] {
										new TemplateType { Name = "test" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("No generic template name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = string.Empty
									}
								}
							}
						}
					}
				},
				"Unnamed generic template [0] for resource 'test' in container 'test'."
			),

			TestCase("Wrong generic template index",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[-2] = "negative"
									}
								}
							}
						}
					}
				},
				"Negative generic template index [-2] for resource 'test' in container 'test'."
			),

			TestCase("Add generic templates",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a",
										[1] = "b"
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									GenericTemplates = new[] {
										new TemplateType { Name = "a" },
										new TemplateType { Name = "b" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add generic template twice",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "test"
									}
								},
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "test"
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									GenericTemplates = new[] {
										new TemplateType { Name = "test" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add generic templates across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[0] = "a",
											[1] = "b"
										}
									},
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[2] = "c"
										}
									}
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[3] = "d"
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[4] = "e"
										}
									}
								}
							},
							new PartialContainer {
								Name = "other",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[0] = "f"
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									GenericTemplates = new[] {
										new TemplateType { Name = "a" },
										new TemplateType { Name = "b" },
										new TemplateType { Name = "c" },
										new TemplateType { Name = "d" },
										new TemplateType { Name = "e" }
									}
								}
							}
						},
						new Container {
							Name = "other",
							Resources = new[] {
								new Resource {
									Name = "test",
									GenericTemplates = new[] {
										new TemplateType { Name = "f" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Duplicate generic template",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a",
										[1] = "a"
									}
								}
							}
						}
					}
				},
				"Duplicated generic template 'a' for resource 'test' in container 'test'."
			),

			TestCase("Duplicate generic templates across resources",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a"
									}
								},
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[1] = "a"
									}
								}
							}
						}
					}
				},
				"Duplicated generic template 'a' for resource 'test' in container 'test'."
			),

			TestCase("Duplicate generic templates across containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a"
									}
								}
							}
						},
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[1] = "a"
									}
								}
							}
						}
					}
				},
				"Duplicated generic template 'a' for resource 'test' in container 'test'."
			),

			TestCase("Duplicate generic templates across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[0] = "a"
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[1] = "a"
										}
									}
								}
							}
						}
					}
				},
				"Duplicated generic template 'a' for resource 'test' in container 'test'."
			),

			TestCase("Different generic templates across resources",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a"
									}
								},
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "b"
									}
								}
							}
						}
					}
				},
				"Different generic templates [0] for resource 'test' in container 'test'."
			),

			TestCase("Different generic templates across containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "a"
									}
								}
							}
						},
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[0] = "b"
									}
								}
							}
						}
					}
				},
				"Different generic templates [0] for resource 'test' in container 'test'."
			),

			TestCase("Different generic templates across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[0] = "a"
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										GenericTemplates = {
											[0] = "b"
										}
									}
								}
							}
						}
					}
				},
				"Different generic templates [0] for resource 'test' in container 'test'."
			),

			TestCase("Miss generic template",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									GenericTemplates = {
										[1] = "test"
									}
								}
							}
						}
					}
				},
				"Missing generic template [0] for resource 'test' in container 'test'."
			),

			// Built-in Types Override
			// ----

			TestCase("Override built-in type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource { Name = "bool" }
							}
						}
					}
				},
				"Overriding built-in resource 'bool' is denied."
			),

			TestCase("Override built-in type with different generic templates",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "duration",
									GenericTemplates = {
										[0] = "unit"
									}
								}
							}
						}
					}
				},
				"Overriding built-in resource 'duration' is denied."
			),

			TestCase("Override built-in collection type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource { Name = "set" }
							}
						}
					}
				},
				"Overriding built-in resource 'set' is denied."
			),

			TestCase("Override built-in collection type with different generic templates",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "set",
									GenericTemplates = {
										[0] = "int",
										[1] = "float"
									}
								}
							}
						}
					}
				},
				"Overriding built-in resource 'set' is denied."
			),

			// Simple Fields
			// ----

			TestCase("Add fields with built-in types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["b"] = new PartialFieldType { Name = "bool" },
										["i"] = new PartialFieldType { Name = "int" },
										["f"] = new PartialFieldType { Name = "float" },
										["c"] = new PartialFieldType { Name = "char" },
										["s"] = new PartialFieldType { Name = "string" },
										["d"] = new PartialFieldType { Name = "date" },
										["t"] = new PartialFieldType { Name = "datetime" },
										["p"] = new PartialFieldType { Name = "duration" }
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["b"] = new BoolType(),
										["i"] = new IntType(),
										["f"] = new FloatType(),
										["c"] = new CharType(),
										["s"] = new StringType(),
										["d"] = new DateType(),
										["t"] = new DatetimeType(),
										["p"] = new DurationType()
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add fields with built-in collection types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["l"] = new PartialFieldType {
											Name = "list",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										},
										["s"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										},
										["m"] = new PartialFieldType {
											Name = "map",
											Generics = {
												[0] = new PartialFieldType { Name = "string" },
												[1] = new PartialFieldType { Name = "int" }
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["l"] = new ListType(new IntType()),
										["s"] = new SetType(new IntType()),
										["m"] = new MapType(new StringType(), new IntType())
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add fields across resources, containers and contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["a"] = new PartialFieldType { Name = "int" }
										}
									},
									new PartialResource {
										Name = "test",
										Fields = {
											["b"] = new PartialFieldType { Name = "float" }
										}
									}
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["a"] = new PartialFieldType { Name = "int" },
											["c"] = new PartialFieldType { Name = "char" }
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["b"] = new PartialFieldType { Name = "float" },
											["d"] = new PartialFieldType { Name = "string" }
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["a"] = new IntType(),
										["b"] = new FloatType(),
										["c"] = new CharType(),
										["d"] = new StringType()
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with custom types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["c"] = new PartialFieldType { Name = "cat" }
									}
								},
								new PartialResource {
									Name = "cat",
									Fields = {
										["id"] = new PartialFieldType {
											Name = "int",
											Meta = {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["c"] = new ResourceType { Name = "cat" }
									}
								},
								new Resource {
									Name = "cat",
									Fields = new Dictionary<string, FieldType> {
										["id"] = new IntType {
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add fields with custom generic types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["d"] = new PartialFieldType {
											Name = "hateoas",
											Generics =  {
												[0] = new PartialFieldType { Name = "cat" }
											}
										},
										["e"] = new PartialFieldType {
											Name = "hateoas",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								},
								new PartialResource {
									Name = "hateoas",
									GenericTemplates = {
										[0] = "wrapped"
									},
									Fields = {
										["data"] = new PartialFieldType { Name = "wrapped" }
									}
								},
								new PartialResource {
									Name = "cat",
									Fields = {
										["id"] = new PartialFieldType {
											Name = "int",
											Meta = {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["d"] = new ResourceType {
											Name = "hateoas",
											Generics = new[] {
												new ResourceType { Name = "cat" }
											}
										},
										["e"] = new ResourceType {
											Name = "hateoas",
											Generics = new[] {
												new IntType()
											}
										}
									}
								},
								new Resource {
									Name = "hateoas",
									GenericTemplates = new[] {
										new TemplateType { Name = "wrapped" }
									},
									Fields = new Dictionary<string, FieldType> {
										["data"] = new TemplateType { Name = "wrapped" }
									}
								},
								new Resource {
									Name = "cat",
									Fields = new Dictionary<string, FieldType> {
										["id"] = new IntType {
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with missing types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = "missing" }
									}
								}
							}
						}
					}
				},
				// Missing types are not automatically added to the container,
				// nor do they error out, because they might come from somewhere else.
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new ResourceType { Name = "missing" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with missing generic types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "missing",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								}
							}
						}
					}
				},
				// Missing types are not automatically added to the container,
				// nor do they error out, because they might come from somewhere else.
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new ResourceType {
											Name = "missing",
											Generics = new[] {
												new IntType()
											}
										}
									}
								}
							}
						}
					}
				}
			),

			TestCase("No field name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										[string.Empty] = new PartialFieldType { Name = "int" }
									}
								}
							}
						}
					}
				},
				"Unnamed field in resource 'test' of container 'test'."
			),

			TestCase("No field type name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = string.Empty }
									}
								}
							}
						}
					}
				},
				"Unnamed type for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("No field type generic name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType { Name = string.Empty }
											}
										}
									}
								}
							}
						}
					}
				},
				"Unnamed type for field 'x' in resource 'test' of container 'test'."
			),
			
			TestCase("No field type generic generic name",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "set",
													Generics = {
														[0] = new PartialFieldType { Name = string.Empty }
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Unnamed type for field 'x' in resource 'test' of container 'test'."
			),

			// Built-in Genrics
			// ----

			TestCase("Extra generic for built-in type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["i"] = new PartialFieldType {
											Name = "int",
											Generics = {
												[0] = new PartialFieldType { Name = "string" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Extra generic [0] for field 'i' in resource 'test' of container 'test'."
			),

			TestCase("No generic for built-in set type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["s"] = new PartialFieldType { Name = "set" }
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 's' in resource 'test' of container 'test'."
			),

			TestCase("No generic for built-in list type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["l"] = new PartialFieldType { Name = "list" }
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 'l' in resource 'test' of container 'test'."
			),

			TestCase("No key generic for built-in map type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["m"] = new PartialFieldType {
											Name = "map",
											Generics = {
												[1] = new PartialFieldType { Name = "int" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 'm' in resource 'test' of container 'test'."
			),

			TestCase("No value generic for built-in map type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["m"] = new PartialFieldType {
											Name = "map",
											Generics = {
												[0] = new PartialFieldType { Name = "string" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Missing generic [1] for field 'm' in resource 'test' of container 'test'."
			),

			TestCase("Extra generic for built-in set type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["s"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType { Name = "int" },
												[1] = new PartialFieldType { Name = "float" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Extra generic [1] for field 's' in resource 'test' of container 'test'."
			),

			TestCase("Extra generic for built-in list type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["l"] = new PartialFieldType {
											Name = "list",
											Generics = {
												[0] = new PartialFieldType { Name = "int" },
												[1] = new PartialFieldType { Name = "float" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Extra generic [1] for field 'l' in resource 'test' of container 'test'."
			),

			TestCase("Extra generic for built-in map type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["m"] = new PartialFieldType {
											Name = "map",
											Generics = {
												[0] = new PartialFieldType { Name = "string" },
												[1] = new PartialFieldType { Name = "int" },
												[2] = new PartialFieldType { Name = "char" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Extra generic [2] for field 'm' in resource 'test' of container 'test'."
			),

			// Custom Generics
			// ----

			TestCase("No generic for custom type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["h"] = new PartialFieldType {
											Name = "hateoas" }
									}
								},
								new PartialResource {
									Name = "hateoas",
									GenericTemplates = {
										[0] = "wrapped"
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 'h' in resource 'test' of container 'test'."
			),

			TestCase("Miss generic for custom type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["t"] = new PartialFieldType {
											Name = "tuple",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								},
								new PartialResource {
									Name = "tuple",
									GenericTemplates = {
										[0] = "a",
										[1] = "b"
									}
								}
							}
						}
					}
				},
				"Missing generic [1] for field 't' in resource 'test' of container 'test'."
			),

			TestCase("Extra generic for custom type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["h"] = new PartialFieldType {
											Name = "hateoas",
											Generics = {
												[0] = new PartialFieldType { Name = "int" },
												[1] = new PartialFieldType { Name = "float" }
											}
										}
									}
								},
								new PartialResource {
									Name = "hateoas",
									GenericTemplates = {
										[0] = "wrapped"
									}
								}
							}
						}
					}
				},
				"Extra generic [1] for field 'h' in resource 'test' of container 'test'."
			),

			// Wrong Fields
			// ----

			TestCase("Different field type names across types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = "int" }
									}
								},
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = "float" }
									}
								}
							}
						}
					}
				},
				"Different types for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type names across containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = "int" }
									}
								}
							}
						},
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType { Name = "float" }
									}
								}
							}
						}
					}
				},
				"Different types for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type names across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType { Name = "int" }
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType { Name = "float" }
										}
									}
								}
							}
						}
					}
				},
				"Different types for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type generics across types",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								},
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Generics = {
												[0] = new PartialFieldType { Name = "float" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Different generics [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type generics across containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								}
							}
						},
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Generics = {
												[0] = new PartialFieldType { Name = "float" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Different generics [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type generics across contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType { Name = "int" }
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType { Name = "float" }
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different generics [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type generics generics",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Name = "set",
														Generics = {
															[0] = new PartialFieldType { Name = "int" }
														}
													}
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType {
														Generics = {
															[0] = new PartialFieldType { Name = "float" }
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different generics [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field type generics generics generics",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Name = "set",
														Generics = {
															[0] = new PartialFieldType {
																Generics = {
																	[0] = new PartialFieldType { Name = "int" }
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType {
														Generics = {
															[0] = new PartialFieldType {
																Name = "set",
																Generics = {
																	[0] = new PartialFieldType { Name = "float" }
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different generics [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Miss field type generics",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "map",
													Generics = {
														[1] = new PartialFieldType { Name = "int" }
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Miss field type generics generics",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "set",
													Generics = {
														[0] = new PartialFieldType {
															Name = "map",
															Generics = {
																[1] = new PartialFieldType { Name = "int" }
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Missing generic [0] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Wrong generic index",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "cat",
											Generics = {
												[-2] = new PartialFieldType { Name = "string" }
											}
										}
									}
								}
							}
						}
					}
				},
				"Negative generic index [-2] for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Wrong generic generic index",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "cat",
													Generics = {
														[-2] = new PartialFieldType { Name = "string" }
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Negative generic index [-2] for field 'x' in resource 'test' of container 'test'."
			),

			// Field Meta
			// ----

			TestCase("Add field meta across resources, containers and contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "int",
												Meta = {
													[NapBuiltInMeta.Min] = 1,
													[NapBuiltInMeta.MinIsInclusive] = false
												}
											}
										}
									},
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Meta = {
													[NapBuiltInMeta.Max] = 42
												}
											}
										}
									}
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Meta = {
													[NapBuiltInMeta.Default] = 12
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Meta = {
													[NapBuiltInMeta.IsOptional] = true
												}
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new IntType {
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.Min] = 1,
												[NapBuiltInMeta.MinIsInclusive] = false,
												[NapBuiltInMeta.Max] = 42,
												[NapBuiltInMeta.Default] = 12,
												[NapBuiltInMeta.IsOptional] = true
											}
										}
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field generics meta across resources, containers and contexts",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType {
														Name = "int",
														Meta = {
															[NapBuiltInMeta.Min] = 1,
															[NapBuiltInMeta.MinIsInclusive] = false
														}
													}
												}
											}
										}
									},
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Meta = {
															[NapBuiltInMeta.Max] = 42
														}
													}
												}
											}
										}
									}
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Meta = {
															[NapBuiltInMeta.Default] = 12
														}
													}
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Meta = {
															[NapBuiltInMeta.IsOptional] = true
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new SetType(new IntType {
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.Min] = 1,
												[NapBuiltInMeta.MinIsInclusive] = false,
												[NapBuiltInMeta.Max] = 42,
												[NapBuiltInMeta.Default] = 12,
												[NapBuiltInMeta.IsOptional] = true
											}
										})
									}
								}
							}
						}
					}
				}
			),

			TestCase("Different field meta across resources",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "int",
											Meta = {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								},
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Meta = {
												[NapBuiltInMeta.Min] = 2
											}
										}
									}
								}
							}
						}
					}
				},
				"Different meta 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field meta across containers",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Meta = {
												[NapBuiltInMeta.Min] = 1
											}
										}
									}
								}
							}
						},
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "int",
											Meta = {
												[NapBuiltInMeta.Min] = 2
											}
										}
									}
								}
							}
						}
					}
				},
				"Different meta 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field meta across contexts",
				new [] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "int",
												Meta = {
													[NapBuiltInMeta.Min] = 1
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Meta = {
													[NapBuiltInMeta.Min] = 2
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different meta 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field generics meta",
				new [] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType {
														Meta = {
															[NapBuiltInMeta.Min] = 1
														}
													}
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Name = "int",
														Meta = {
															[NapBuiltInMeta.Min] = 2
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different meta 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Different field generics generics meta",
				new [] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Name = "set",
												Generics = {
													[0] = new PartialFieldType {
														Generics = {
															[0] = new PartialFieldType {
																Name = "int",
																Meta = {
																	[NapBuiltInMeta.Min] = 1
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "test",
										Fields = {
											["x"] = new PartialFieldType {
												Generics = {
													[0] = new PartialFieldType {
														Name = "set",
														Generics = {
															[0] = new PartialFieldType {
																Meta = {
																	[NapBuiltInMeta.Min] = 2
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Different meta 'min' for field 'x' in resource 'test' of container 'test'."
			),

			// Wrong Field Meta
			// ----

			TestCase("Wrong field meta",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "int",
											Meta = {
												[NapBuiltInMeta.Min] = 1.25f
											}
										}
									}
								}
							}
						}
					}
				},
				"Wrong meta value 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Wrong field generic meta",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "int",
													Meta = {
														[NapBuiltInMeta.Min] = 1.25f
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Wrong meta value 'min' for field 'x' in resource 'test' of container 'test'."
			),

			TestCase("Wrong field generic generic meta",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "set",
											Generics = {
												[0] = new PartialFieldType {
													Name = "set",
													Generics = {
														[0] = new PartialFieldType {
															Name = "int",
															Meta = {
																[NapBuiltInMeta.Min] = 1.25f
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				},
				"Wrong meta value 'min' for field 'x' in resource 'test' of container 'test'."
			),

			// Misc, Decisions, and To-do's
			// ----

			TestCase("Unknown field meta",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "int",
											Meta = {
												["yolo"] = "troll"
											}
										}
									}
								}
							}
						}
					}
				},
				// Extra meta is perfectly valid.
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new IntType {
											Meta = new Dictionary<string, object?> {
												["yolo"] = "troll"
											}
										}
									}
								}
							}
						}
					}
				}
			),

			// Should we consider a type different by its generics as a missing type?
			// In this case, there should be no exception there.

			TestCase("Merge container",
				new[] {
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "a",
										Fields = {
											["x"] = new PartialFieldType { Name = "int" },
											["y"] = new PartialFieldType { Name = "float" }
										}
									},
									new PartialResource {
										Name = "b",
										Fields = {
											["t"] = new PartialFieldType { Name = "char" },
											["u"] = new PartialFieldType { Name = "string" }
										}
									}
								}
							},
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "a",
										Fields = {
											["y"] = new PartialFieldType { Meta = { [NapBuiltInMeta.Min] = 42f } },
											["z"] = new PartialFieldType { Name = "duration" }
										}
									}
								}
							}
						}
					},
					new PartialContext {
						Name = "test",
						Containers = {
							new PartialContainer {
								Name = "test",
								Resources = {
									new PartialResource {
										Name = "a",
										Fields = {
											["y"] = new PartialFieldType { Meta = { [NapBuiltInMeta.Min] = 42f } },
											["z"] = new PartialFieldType { Name = "duration" }
										}
									},
									new PartialResource {
										Name = "c",
										Fields = {
											["v"] = new PartialFieldType { Name = "bool" }
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "a",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new IntType(),
										["y"] = new FloatType {
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.Min] = 42f
											}
										},
										["z"] = new DurationType()
									}
								},
								new Resource {
									Name = "b",
									Fields = new Dictionary<string, FieldType> {
										["t"] = new CharType(),
										["u"] = new StringType()
									}
								},
								new Resource {
									Name = "c",
									Fields = new Dictionary<string, FieldType> {
										["v"] = new BoolType()
									}
								}
							}
						}
					}
				}
			),

			TestCase("Recursive custom type",
				new PartialContext {
					Name = "test",
					Containers = {
						new PartialContainer {
							Name = "test",
							Resources = {
								new PartialResource {
									Name = "test",
									Fields = {
										["x"] = new PartialFieldType {
											Name = "btree",
											Generics = {
												[0] = new PartialFieldType { Name = "int" }
											}
										}
									}
								},
								new PartialResource {
									Name = "btree",
									GenericTemplates = {
										[0] = "T"
									},
									Fields = {
										["root"] = new PartialFieldType {
											Name = "T",
											Meta = {
												[NapBuiltInMeta.IsOptional] = true
											}
										},
										["left"] = new PartialFieldType {
											Name = "btree",
											Generics = {
												[0] = new PartialFieldType { Name = "T" }
											},
											Meta = {
												[NapBuiltInMeta.IsOptional] = true,
												[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
											}
										},
										["right"] = new PartialFieldType {
											Name = "btree",
											Generics = {
												[0] = new PartialFieldType { Name = "T" }
											},
											Meta = {
												[NapBuiltInMeta.IsOptional] = true,
												[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
											}
										}
									}
								}
							}
						}
					}
				},
				new Context {
					Name = "test",
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "test",
									Fields = new Dictionary<string, FieldType> {
										["x"] = new ResourceType {
											Name = "btree",
											Generics = new[] {
												new IntType()
											}
										}
									}
								},
								new Resource {
									Name = "btree",
									GenericTemplates = new[] {
										new TemplateType { Name = "T" }
									},
									Fields = new Dictionary<string, FieldType> {
										["root"] = new TemplateType {
											Name = "T",
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.IsOptional] = true
											}
										},
										["left"] = new ResourceType {
											Name = "btree",
											Generics = new[] {
												new TemplateType { Name = "T" }
											},
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.IsOptional] = true,
												[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
											}
										},
										["right"] = new ResourceType {
											Name = "btree",
											Generics = new[] {
												new TemplateType { Name = "T" }
											},
											Meta = new Dictionary<string, object?> {
												[NapBuiltInMeta.IsOptional] = true,
												[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
											}
										}
									}
								}
							}
						}
					}
				}
			)

			/*

			Extra/Less fields generics' generics (and recursively)
				=> consider missing type? @see extra generic test case

			*/
		};

		// Assertions
		// ----

		static void AssertContext(Context expected, Context actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Context names didn't match.");
			Assert.AreEqual(expected.Containers.Count, actual.Containers.Count, "Containers are not the same count.");

			using var expectedEnumerator = expected.Containers.GetEnumerator();
			using var actualEnumerator = actual.Containers.GetEnumerator();

			while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
				AssertContainer(expectedEnumerator.Current, actualEnumerator.Current);
		}

		static void AssertContainer(Container expected, Container actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Container names didn't match.");
			Assert.AreEqual(expected.Resources.Count, actual.Resources.Count, "Resources are not the same count.");

			using var expectedEnumerator = expected.Resources.GetEnumerator();
			using var actualEnumerator = actual.Resources.GetEnumerator();

			while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
				AssertResource(expectedEnumerator.Current, actualEnumerator.Current);
		}

		static void AssertResource(Resource expected, Resource actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Resource names didn't match.");
			Assert.AreEqual(expected.GenericTemplates.Count, actual.GenericTemplates.Count, "Resource's generic templatess are not the same count.");
			Assert.AreEqual(expected.Fields.Count, actual.Fields.Count, "Resource's fields are not the same count.");

			for (int i = 0; i < actual.GenericTemplates.Count; ++i)
				AssertTemplateType(expected.GenericTemplates[i], actual.GenericTemplates[i]);

			for (int i = 0; i < actual.Fields.Count; ++i) {
				Assert.AreEqual(expected.Fields.ElementAt(i).Key, actual.Fields.ElementAt(i).Key);
				AssertType(expected.Fields.ElementAt(i).Value, actual.Fields.ElementAt(i).Value);
			}
		}

		static void AssertMeta(string meta, FieldType expected, FieldType actual) {
			var expectedPresence = expected.Meta.TryGetValue(meta, out var expectdValue);
			var actualPresence = actual.Meta.TryGetValue(meta, out var actualValue);

			Assert.AreEqual(expectedPresence, actualPresence, $"'{ meta } meta presences didn't match.");

			if (expectedPresence) {
				if (expectdValue is IEnumerable) {
					Assert.IsInstanceOf<IEnumerable>(actualValue);
					CollectionAssert.AreEqual((IEnumerable) expectdValue, (IEnumerable) actualValue, $"'{ meta } meta values didn't match.");
				}

				else Assert.AreEqual(expectdValue, actualValue, $"'{ meta } meta values didn't match.");
			}
		}

		static void AssertType(FieldType expected, FieldType actual) {
			Assert.IsInstanceOf(expected.GetType(), actual, "Data types didn't match.");

			AssertMeta(NapBuiltInMeta.SameAs, expected, actual);
			AssertMeta(NapBuiltInMeta.NotSameAs, expected, actual);

			switch (actual) {
				case BoolType actualBool: AssertBoolType((BoolType) expected, actualBool); break;
				case IntType actualInt: AssertIntType((IntType) expected, actualInt); break;
				case FloatType actualFloat: AssertFloatType((FloatType) expected, actualFloat); break;
				case CharType actualChar: AssertCharType((CharType) expected, actualChar); break;
				case StringType actualString: AssertStringType((StringType) expected, actualString); break;
				case DateType actualDate: AssertDateType((DateType) expected, actualDate); break;
				case DatetimeType actualDatetime: AssertDatetimeType((DatetimeType) expected, actualDatetime); break;
				case DurationType actualDuration: AssertDurationType((DurationType) expected, actualDuration); break;
				case SetType actualSet: AssertSetType((SetType) expected, actualSet); break;
				case ListType actualList: AssertListType((ListType) expected, actualList); break;
				case MapType actualMap: AssertMapType((MapType) expected, actualMap); break;
				case TemplateType actualTemplate: AssertTemplateType((TemplateType) expected, actualTemplate); break;
				case ResourceType actualresource: AssertResourceType((ResourceType) expected, actualresource); break;

				default: Assert.Fail("WTF, did I miss a type?"); break;
			};
		}

		static void AssertTemplateType(TemplateType expected, TemplateType actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Type Name's didn't match.");

			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertBoolType(BoolType expected, BoolType actual) {
			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertIntType(IntType expected, IntType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertFloatType(FloatType expected, FloatType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertCharType(CharType expected, CharType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertStringType(StringType expected, StringType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);
			AssertMeta(NapBuiltInMeta.Pattern, expected, actual);

			AssertMeta(NapBuiltInMeta.AllowEmpty, expected, actual);
			AssertMeta(NapBuiltInMeta.AllowMultiline, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertDateType(DateType expected, DateType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertDatetimeType(DatetimeType expected, DatetimeType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertDurationType(DurationType expected, DurationType actual) {
			AssertMeta(NapBuiltInMeta.AllowedValues, expected, actual);
			AssertMeta(NapBuiltInMeta.ForbiddenValues, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.Default, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);
		}

		static void AssertSetType(SetType expected, SetType actual) {
			AssertType(expected.ElementType, actual.ElementType);

			AssertMeta(NapBuiltInMeta.AllowDuplicates, expected, actual);
			AssertMeta(NapBuiltInMeta.AllowEmpty, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);
		}

		static void AssertListType(ListType expected, ListType actual) {
			AssertType(expected.ElementType, actual.ElementType);

			AssertMeta(NapBuiltInMeta.AllowDuplicates, expected, actual);
			AssertMeta(NapBuiltInMeta.AllowEmpty, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);
		}

		static void AssertMapType(MapType expected, MapType actual) {
			AssertType(expected.KeyType, actual.KeyType);
			AssertType(expected.ElementType, actual.ElementType);

			AssertMeta(NapBuiltInMeta.AllowDuplicates, expected, actual);
			AssertMeta(NapBuiltInMeta.AllowEmpty, expected, actual);

			AssertMeta(NapBuiltInMeta.Min, expected, actual);
			AssertMeta(NapBuiltInMeta.MinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.Max, expected, actual);
			AssertMeta(NapBuiltInMeta.MaxIsInclusive, expected, actual);

			AssertMeta(NapBuiltInMeta.ExclusionMin, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMinIsInclusive, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMax, expected, actual);
			AssertMeta(NapBuiltInMeta.ExclusionMaxIsInclusive, expected, actual);
		}

		static void AssertResourceType(ResourceType expected, ResourceType actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Type Name's didn't match.");

			AssertMeta(NapBuiltInMeta.SelfReference, expected, actual);
			AssertMeta(NapBuiltInMeta.IsOptional, expected, actual);

			Assert.AreEqual(expected.Generics.Count, actual.Generics.Count, "Type's generics are not the same count.");

			for (int i = 0; i < actual.Generics.Count; ++i)
				AssertType(expected.Generics[i], actual.Generics[i]);
		}
	}
}
