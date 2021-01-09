using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Nap.Tests {
	static class BuilderTests {
		[Test]
		[TestCaseSource(nameof(TestCases))]
		public static void Tests(Action<IContextBuilder> builderInit, object expected) {
			if (
				typeof(IContextBuilder)
					.Assembly
					.GetType("Nap.ContextBuilder")
					?.GetConstructor(Array.Empty<Type>())
					?.Invoke(Array.Empty<object>()) is IContextBuilder context
			) {
				if (expected is Context expectedBuildResult) {
					builderInit(context);
					var actualBuildResult = context.Build();

					AssertContext(expectedBuildResult, actualBuildResult);
				}

				else if (expected is string expectedExceptionMessage) {
					var actualException = Assert.Throws<NapContextException>(() => {
						builderInit(context);
						context.Build();
					});

					Assert.AreEqual(expectedExceptionMessage, actualException.Message);
				}
			}

			else Assert.Fail("Reflection looks fucked up...");
		}

		static TestCaseData TestCase(string name, Action<IContextBuilder> builder, Context expected) =>
			new TestCaseData(builder, expected).SetName(name);

		static TestCaseData TestCase(string name, Action<IContextBuilder> builder, string expectedExceptionMessage) =>
			new TestCaseData(builder, expectedExceptionMessage).SetName(name);

		static readonly IEnumerable<object> TestCases = new[] {
			// Context
			// ----

			TestCase("Add context name",
				context => context.Name = "test",
				new Context { Name = "test" }
			),

			// Containers
			// ----

			TestCase("Add container",
				context => {
					context.AddContainer(container => container.Name = "test");
				},
				new Context {
					Containers = new[] {
						new Container { Name = "test" }
					}
				}
			),

			TestCase("Add containers",
				context => {
					context.AddContainer(container => container.Name = "a");
					context.AddContainer(container => container.Name = "b");
				},
				new Context {
					Containers = new[] {
						new Container { Name = "a" },
						new Container { Name = "b" }
					}
				}
			),

			TestCase("Add container twice",
				context => {
					context.AddContainer(container => container.Name = "test");
					context.AddContainer(container => container.Name = "test");
				},
				new Context {
					Containers = new[] {
						new Container { Name = "test" }
					}
				}
			),

			// Types
			// ----

			TestCase("Add type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "test");
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource { Name = "test" }
							}
						}
					}
				}
			),

			TestCase("Add types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "a");
						container.AddResource(type => type.Name = "b");
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource { Name = "a" },
								new Resource { Name = "b" }
							}
						}
					}
				}
			),

			TestCase("Add type twice",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "test");
						container.AddResource(type => type.Name = "test");
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource { Name = "test" }
							}
						}
					}
				}
			),

			TestCase("Add types across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "a");
						container.AddResource(type => type.Name = "b");
					});

					context.AddContainer(container => {
						container.AddResource(type => type.Name = "c");
						container.AddResource(type => type.Name = "a");
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource { Name = "a" },
								new Resource { Name = "b" },
								new Resource { Name = "c" }
							}
						}
					}
				}
			),

			// Generic Templates
			// ----

			TestCase("Add generic template",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "test"));
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									GenericTemplates = new[] {
										new TemplateType { Name = "test" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add generic templates",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(1, "b");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
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
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(0, "a");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									GenericTemplates = new[] {
										new TemplateType { Name = "a" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add generic templates across types and containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(1, "b");
						});

						container.AddResource(type => type.SetGenericTemplate(2, "c"));
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(1, "b");
							type.SetGenericTemplate(2, "c");
							type.SetGenericTemplate(3, "d");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									GenericTemplates = new[] {
										new TemplateType { Name = "a" },
										new TemplateType { Name = "b" },
										new TemplateType { Name = "c" },
										new TemplateType { Name = "d" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Duplicate generic template",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(1, "a");
						});
					});
				},
				"Duplicated generic template 'a' for type '' in container ''."
			),

			TestCase("Duplicate generic templates across types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "a"));
						container.AddResource(type => type.SetGenericTemplate(1, "a"));
					});
				},
				"Duplicated generic template 'a' for type '' in container ''."
			),

			TestCase("Duplicate generic templates across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "a"));
					});

					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(1, "a"));
					});
				},
				"Duplicated generic template 'a' for type '' in container ''."
			),

			TestCase("Different generic template",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(0, "b");
						});
					});
				},
				"Different generic templates [0] for type '' in container ''."
			),

			TestCase("Different generic templates across types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "a"));
						container.AddResource(type => type.SetGenericTemplate(0, "b"));
					});
				},
				"Different generic templates [0] for type '' in container ''."
			),

			TestCase("Different generic templates across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "a"));
					});

					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(0, "b"));
					});
				},
				"Different generic templates [0] for type '' in container ''."
			),

			TestCase("Miss generic template",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.SetGenericTemplate(1, "t"));
					});
				},
				"Missing generic template [0] for type '' in container ''."
			),

			// Built-in Types Override
			// ----

			TestCase("Override built-in type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "bool");
					});
				},
				"Overriding built-in type 'bool' is denied."
			),

			TestCase("Override built-in type with different generic templates",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.Name = "duration";
							type.SetGenericTemplate(0, "unit");
						});
					});
				},
				"Overriding built-in type 'duration' is denied."
			),

			TestCase("Override built-in collection type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => type.Name = "set");
					});
				},
				"Overriding built-in type 'set' is denied."
			),

			TestCase("Override built-in collection type with different generic templates",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.Name = "set";
							type.SetGenericTemplate(0, "int");
							type.SetGenericTemplate(1, "float");
						});
					});
				},
				"Overriding built-in type 'set' is denied."
			),

			// Simple Fields
			// ----

			TestCase("Add fields with built-in types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("b", fieldType => fieldType.Name = "bool");
							type.SetFieldType("i", fieldType => fieldType.Name = "int");
							type.SetFieldType("f", fieldType => fieldType.Name = "float");
							type.SetFieldType("c", fieldType => fieldType.Name = "char");
							type.SetFieldType("s", fieldType => fieldType.Name = "string");
							type.SetFieldType("d", fieldType => fieldType.Name = "date");
							type.SetFieldType("t", fieldType => fieldType.Name = "datetime");
							type.SetFieldType("p", fieldType => fieldType.Name = "duration");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
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
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("l", fieldType => {
								fieldType.Name = "list";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});

							type.SetFieldType("s", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});

							type.SetFieldType("m", fieldType => {
								fieldType.Name = "map";
								fieldType.SetGeneric(0, generic => generic.Name = "string");
								fieldType.SetGeneric(1, generic => generic.Name = "int");
							});
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
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

			TestCase("Add fields across types and containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
						});

						container.AddResource(type => {
							type.SetFieldType("y", fieldType => fieldType.Name = "float");
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
							type.SetFieldType("z", fieldType => fieldType.Name = "char");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
										["x"] = new IntType(),
										["y"] = new FloatType(),
										["z"] = new CharType()
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with custom types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("c", fieldType => fieldType.Name = "cat");
						});

						container.AddResource(type => {
							type.Name = "cat";
							type.SetFieldType("id", idType => {
								idType.Name = "int";
								idType.SetMin(1);
							});
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
										["c"] = new ResourceType { Name = "cat" }
									}
								},
								new Resource {
									Name = "cat",
									Fields = new Dictionary<string, DataType> {
										["id"] = new IntType { Min = 1 }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add fields with custom generic types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("d", fieldType => {
								fieldType.Name = "hateoas";
								fieldType.SetGeneric(0, generic => generic.Name = "cat");
							});

							type.SetFieldType("e", fieldType => {
								fieldType.Name = "hateoas";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});
						});

						container.AddResource(type => {
							type.Name = "hateoas";
							type.SetGenericTemplate(0, "wrapped");
							type.SetFieldType("data", fieldType => fieldType.Name = "wrapped");
						});

						container.AddResource(type => {
							type.Name = "cat";
							type.SetFieldType("id", idType => {
								idType.Name = "int";
								idType.SetMin(1);
							});
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
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
									Fields = new Dictionary<string, DataType> {
										["data"] = new TemplateType { Name = "wrapped" }
									}
								},
								new Resource {
									Name = "cat",
									Fields = new Dictionary<string, DataType> {
										["id"] = new IntType { Min = 1 }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with missing types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "missing");
						});
					});
				},
				// Missing types are not automatically added to the container,
				// nor do they error out, because they might come from somewhere else.
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
										["x"] = new ResourceType { Name = "missing" }
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field with missing types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "missing";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});
						});
					});
				},
				// Missing types are not automatically added to the container,
				// nor do they error out, because they might come from somewhere else.
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
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

			// Built-in Genrics
			// ----

			TestCase("Extra generic for built-in type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("i", fieldType => {
								fieldType.Name = "int";
								fieldType.SetGeneric(0, generic => generic.Name = "string");
							});
						});
					});
				},
				"Extra generic [0] for field 'i' in type '' of container ''."
			),
			
			TestCase("No generic for built-in set type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("s", fieldType => fieldType.Name = "set");
						});
					});
				},
				"Missing generic [0] for field 's' in type '' of container ''."
			),

			TestCase("No generic for built-in list type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("l", fieldType => fieldType.Name = "list");
						});
					});
				},
				"Missing generic [0] for field 'l' in type '' of container ''."
			),

			TestCase("No key generic for built-in map type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("m", fieldType => {
								fieldType.Name = "map";
								fieldType.SetGeneric(1, generic => generic.Name = "int");
							});
						});
					});
				},
				"Missing generic [0] for field 'm' in type '' of container ''."
			),

			TestCase("No value generic for built-in map type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("m", fieldType => {
								fieldType.Name = "map";
								fieldType.SetGeneric(0, generic => generic.Name = "string");
							});
						});
					});
				},
				"Missing generic [1] for field 'm' in type '' of container ''."
			),

			TestCase("Extra generic for built-in set type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("s", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
								fieldType.SetGeneric(1, generic => generic.Name = "char");
							});
						});
					});
				},
				"Extra generic [1] for field 's' in type '' of container ''."
			),

			TestCase("Extra generic for built-in list type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("l", fieldType => {
								fieldType.Name = "list";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
								fieldType.SetGeneric(1, generic => generic.Name = "char");
							});
						});
					});
				},
				"Extra generic [1] for field 'l' in type '' of container ''."
			),

			TestCase("Extra generic for built-in map type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("m", fieldType => {
								fieldType.Name = "map";
								fieldType.SetGeneric(0, generic => generic.Name = "string");
								fieldType.SetGeneric(1, generic => generic.Name = "int");
								fieldType.SetGeneric(2, generic => generic.Name = "char");
							});
						});
					});
				},
				"Extra generic [2] for field 'm' in type '' of container ''."
			),

			// Custom Generics
			// ----

			TestCase("No generic for custom type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("h", fieldType => fieldType.Name = "hateoas");
						});

						container.AddResource(type => {
							type.Name = "hateoas";
							type.SetGenericTemplate(0, "wrapped");
						});
					});
				},
				"Missing generic [0] for field 'h' in type '' of container ''."
			),

			TestCase("Miss generic for custom type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("t", fieldType => {
								fieldType.Name = "tuple";
								fieldType.SetGeneric(1, generic => generic.Name = "int");
							});
						});

						container.AddResource(type => {
							type.Name = "tuple";
							type.SetGenericTemplate(0, "a");
							type.SetGenericTemplate(1, "b");
						});
					});
				},
				"Missing generic [0] for field 't' in type '' of container ''."
			),

			TestCase("Extra generic for custom type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("h", fieldType => {
								fieldType.Name = "hateoas";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
								fieldType.SetGeneric(1, generic => generic.Name = "char");
							});
						});

						container.AddResource(type => {
							type.Name = "hateoas";
							type.SetGenericTemplate(0, "wrapped");
						});
					});
				},
				"Extra generic [1] for field 'h' in type '' of container ''."
			),

			// Wrong Fields
			// ----

			TestCase("Different field type names across fields",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
							type.SetFieldType("x", fieldType => fieldType.Name = "float");
						});
					});
				},
				"Different types for field 'x' in type '' of container ''."
			),

			TestCase("Different field type names across types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "float");
						});
					});
				},
				"Different types for field 'x' in type '' of container ''."
			),

			TestCase("Different field type names across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.Name = "float");
						});
					});
				},
				"Different types for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
								fieldType.SetGeneric(0, generic => generic.Name = "float");
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics across fields",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});

							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.Name = "float");
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics across types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.Name = "float");
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => generic.Name = "int");
							});
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.Name = "float");
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics generics",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.SetGeneric(0, generic => generic.Name = "int");
								});
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => {
									generic.Name = "set";
									generic.SetGeneric(0, generic => generic.Name = "float");
								});
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Different field type generics generics generics",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "set";
									generic.SetGeneric(0, generic => {
										generic.SetGeneric(0, generic => generic.Name = "int");
									});
								});
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => {
									generic.SetGeneric(0, generic => {
										generic.Name = "set";
										generic.SetGeneric(0, generic => generic.Name = "float");
									});
								});
							});
						});
					});
				},
				"Different generics [0] for field 'x' in type '' of container ''."
			),

			TestCase("Miss field type generics",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "map";
									generic.SetGeneric(1, generic => generic.Name = "int");
								});
							});
						});
					});
				},
				"Missing generic [0] for field 'x' in type '' of container ''."
			),

			TestCase("Miss field type generics generics",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name= "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name= "set";
									generic.SetGeneric(0, generic => {
										generic.Name= "map";
										generic.SetGeneric(1, generic => generic.Name = "int");
									});
								});
							});
						});
					});
				},
				"Missing generic [0] for field 'x' in type '' of container ''."
			),

			// Field Constraints
			// ----

			TestCase("Add field constraints across types and containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(1);
								fieldType.SetMinIsInclusive(false);
							});

							type.SetFieldType("x", fieldType => {
								fieldType.SetMax(42);
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.SetDefault(12));
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.SetIsOptional(true));
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
										["x"] = new IntType {
											Min = 1,
											MinIsInclusive = false,
											Max = 42,
											Default = 12,
											IsOptional = true
										}
									}
								}
							}
						}
					}
				}
			),

			TestCase("Add field generics constraints across types and containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "int";
									generic.SetMin(1);
									generic.SetMinIsInclusive(false);
								});
							});

							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.SetMax(42));
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.SetDefault(12));
							});
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => generic.SetIsOptional(true));
							});
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
										["x"] = new SetType(new IntType {
											Min = 1,
											MinIsInclusive = false,
											Max = 42,
											Default = 12,
											IsOptional = true
										})
									}
								}
							}
						}
					}
				}
			),

			TestCase("Different field constraints",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(1);
								fieldType.SetMin(2);
							});
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),
			
			TestCase("Different field constraints across fields",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(1);
							});

							type.SetFieldType("x", fieldType => fieldType.SetMin(2));
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Different field constraints across types",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(1);
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.SetMin(2));
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Different field constraints across containers",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(1);
							});
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.SetMin(2));
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Different field generics constraints",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "int";
									generic.SetMin(1);
								});
							});
						});

						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => {
									generic.Name = "int";
									generic.SetMin(2);
								});
							});
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Different field generics generics constraints",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "set";
									generic.SetGeneric(0, generic => {
										generic.Name = "int";
										generic.SetMin(42);
									});
								});
							});
						});
					});

					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.SetGeneric(0, generic => {
									generic.SetGeneric(0, generic => {
										generic.Name = "int";
										generic.SetMin(12);
									});
								});
							});
						});
					});
				},
				"Different constraints 'min' for field 'x' in type '' of container ''."
			),

			// Wrong Field Constraints
			// ----

			TestCase("Wrong field constraint",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "int";
								fieldType.SetMin(12.5f);
							});
						});
					});
				},
				"Wrong constraint value 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Wrong field generic constraint",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "int";
									generic.SetMin(12.5f);
								});
							});
						});
					});
				},
				"Wrong constraint value 'min' for field 'x' in type '' of container ''."
			),

			TestCase("Wrong field generic generic constraint",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "set";
								fieldType.SetGeneric(0, generic => {
									generic.Name = "set";
									generic.SetGeneric(0, generic => {
										generic.Name = "int";
										generic.SetMin(12.5f);
									});
								});
							});
						});
					});
				},
				"Wrong constraint value 'min' for field 'x' in type '' of container ''."
			),

			// Misc, Decisions, and To-do's
			// ----

			// What do to with non-built-in constraints?
			// Stuff them in an 'ExtraConstraints' dictionary?
			// Only for custom types? Possibly not...
			TestCase("Unknown field constraint",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => fieldType.SetContraint("yolo", 42));
						});
					});
				},
				"Unknown constraint 'yolo' for field 'x' in type '' of container ''."
			)
			.Ignore("TODO"),

			// Should we consider a type differing by its generics as a missing type?
			// In this case, there should be no exception there.

			TestCase("Merge container",
				context => {
					context.AddContainer(container => {
						container.Name = "test";

						container.AddResource(type => {
							type.Name = "a";
							type.SetFieldType("x", fieldType => fieldType.Name = "int");
							type.SetFieldType("y", fieldType => fieldType.Name = "float");
						});

						container.AddResource(type => {
							type.Name = "b";
							type.SetFieldType("t", fieldType => fieldType.Name = "char");
							type.SetFieldType("u", fieldType => fieldType.Name = "string");
						});
					});

					context.AddContainer(container => {
						container.Name = "test";

						container.AddResource(type => {
							type.Name = "a";
							type.SetFieldType("y", fieldType => fieldType.SetMin(42f));
							type.SetFieldType("z", fieldType => fieldType.Name = "duration");
						});

						container.AddResource(type => {
							type.Name = "c";
							type.SetFieldType("v", fieldType => fieldType.Name = "bool");
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Name = "test",
							Resources = new[] {
								new Resource {
									Name = "a",
									Fields = new Dictionary<string, DataType> {
										["x"] = new IntType(),
										["y"] = new FloatType { Min = 42f },
										["z"] = new DurationType()
									}
								},
								new Resource {
									Name = "b",
									Fields = new Dictionary<string, DataType> {
										["t"] = new CharType(),
										["u"] = new StringType()
									}
								},
								new Resource {
									Name = "c",
									Fields = new Dictionary<string, DataType> {
										["v"] = new BoolType()
									}
								}
							}
						}
					}
				}
			),

			TestCase("Recursive custom type",
				context => {
					context.AddContainer(container => {
						container.AddResource(type => {
							type.SetFieldType("x", fieldType => {
								fieldType.Name = "btree";
								fieldType.SetGeneric(0, leafType => leafType.Name = "int");
							});
						});

						container.AddResource(type => {
							type.Name = "btree";

							type.SetGenericTemplate(0, "T");

							type.SetFieldType("root", rootType => {
								rootType.Name = "T";
								rootType.SetIsOptional(true);
							});

							type.SetFieldType("left", leftType => {
								leftType.Name = "btree";
								leftType.SetGeneric(0, genericType => genericType.Name = "T");
								leftType.SetIsOptional(true);
								leftType.SetSelfReference(false);
							});

							type.SetFieldType("right", rightType => {
								rightType.Name = "btree";
								rightType.SetGeneric(0, genericType => genericType.Name = "T");
								rightType.SetIsOptional(true);
								rightType.SetSelfReference(false);
							});
						});
					});
				},
				new Context {
					Containers = new[] {
						new Container {
							Resources = new[] {
								new Resource {
									Fields = new Dictionary<string, DataType> {
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
									Fields = new Dictionary<string, DataType> {
										["root"] = new TemplateType { Name = "T", IsOptional = true },
										["left"] = new ResourceType {
											Name = "btree",
											Generics = new[] {
												new TemplateType { Name = "T" }
											},
											IsOptional = true,
											SelfReference = SelfReference.Forbid
										},
										["right"] = new ResourceType {
											Name = "btree",
											Generics = new[] {
												new TemplateType { Name = "T" }
											},
											IsOptional = true,
											SelfReference = SelfReference.Forbid
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

			for (int i = 0; i < actual.Containers.Count; ++i)
				AssertContainer(expected.Containers[i], actual.Containers[i]);
		}

		static void AssertContainer(Container expected, Container actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Container names didn't match.");
			Assert.AreEqual(expected.Resources.Count, actual.Resources.Count, "Resources are not the same count.");

			for (int i = 0; i < actual.Resources.Count; ++i)
				AssertResource(expected.Resources[i], actual.Resources[i]);
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

		static void AssertType(DataType expected, DataType actual) {
			Assert.IsInstanceOf(expected.GetType(), actual, "Data types didn't match.");

			Assert.AreEqual(expected.SameAs, actual.SameAs, "Type SameAs's didn't match.");
			Assert.AreEqual(expected.NotSameAs, actual.NotSameAs, "Type NotSameAs's didn't match.");

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
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}
		
		static void AssertBoolType(BoolType expected, BoolType actual) {
			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertIntType(IntType expected, IntType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertFloatType(FloatType expected, FloatType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertCharType(CharType expected, CharType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertStringType(StringType expected, StringType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");
			Assert.AreEqual(expected.Pattern, actual.Pattern, "Type Pattern's didn't match.");

			Assert.AreEqual(expected.AllowsEmpty, actual.AllowsEmpty, "Type AllowsEmpty's didn't match.");
			Assert.AreEqual(expected.AllowsMultiline, actual.AllowsMultiline, "Type AllowsMultiline's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertDateType(DateType expected, DateType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertDatetimeType(DatetimeType expected, DatetimeType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertDurationType(DurationType expected, DurationType actual) {
			CollectionAssert.AreEqual(expected.AllowedValues, actual.AllowedValues, "Type AllowedValues's didn't match.");
			CollectionAssert.AreEqual(expected.ForbiddenValues, actual.ForbiddenValues, "Type ForbiddenValues's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.Default, actual.Default, "Type Default's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");
		}

		static void AssertSetType(SetType expected, SetType actual) {
			AssertType(expected.ElementType, actual.ElementType);

			Assert.AreEqual(expected.AllowsDuplicates, actual.AllowsDuplicates, "Type AllowsDuplicates's didn't match.");
			Assert.AreEqual(expected.AllowsEmpty, actual.AllowsEmpty, "Type AllowsEmpty's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");
		}

		static void AssertListType(ListType expected, ListType actual) {
			AssertType(expected.ElementType, actual.ElementType);

			Assert.AreEqual(expected.AllowsDuplicates, actual.AllowsDuplicates, "Type AllowsDuplicates's didn't match.");
			Assert.AreEqual(expected.AllowsEmpty, actual.AllowsEmpty, "Type AllowsEmpty's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");
		}

		static void AssertMapType(MapType expected, MapType actual) {
			AssertType(expected.KeyType, actual.KeyType);
			AssertType(expected.ElementType, actual.ElementType);

			Assert.AreEqual(expected.AllowsDuplicates, actual.AllowsDuplicates, "Type AllowsDuplicates's didn't match.");
			Assert.AreEqual(expected.AllowsEmpty, actual.AllowsEmpty, "Type AllowsEmpty's didn't match.");

			Assert.AreEqual(expected.Min, actual.Min, "Type Min's didn't match.");
			Assert.AreEqual(expected.MinIsInclusive, actual.MinIsInclusive, "Type MinIsInclusive's didn't match.");
			Assert.AreEqual(expected.Max, actual.Max, "Type Max's didn't match.");
			Assert.AreEqual(expected.MaxIsInclusive, actual.MaxIsInclusive, "Type MaxIsInclusive's didn't match.");

			Assert.AreEqual(expected.ExclusionMin, actual.ExclusionMin, "Type ExclusionMin's didn't match.");
			Assert.AreEqual(expected.ExclusionMinIsInclusive, actual.ExclusionMinIsInclusive, "Type ExclusionMinIsInclusive's didn't match.");
			Assert.AreEqual(expected.ExclusionMax, actual.ExclusionMax, "Type ExclusionMax's didn't match.");
			Assert.AreEqual(expected.ExclusionMaxIsInclusive, actual.ExclusionMaxIsInclusive, "Type ExclusionMaxIsInclusive's didn't match.");
		}

		static void AssertResourceType(ResourceType expected, ResourceType actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Type Name's didn't match.");
			Assert.AreEqual(expected.SelfReference, actual.SelfReference, "Type SelfReference's didn't match.");
			Assert.AreEqual(expected.IsOptional, actual.IsOptional, "Type IsOptional's didn't match.");

			Assert.AreEqual(expected.Generics.Count, actual.Generics.Count, "Type's generics are not the same count.");

			for (int i = 0; i < actual.Generics.Count; ++i)
				AssertType(expected.Generics[i], actual.Generics[i]);
		}
	}
}
