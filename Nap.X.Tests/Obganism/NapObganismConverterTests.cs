using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Nap.Obganism.Tests {
	static class NapObganismConverterTests {
		[Test]
		[TestCaseSource(nameof(SingleResourceTestCases))]
		public static void SingleResourceTests(Resource resource, string expected) {
			var actual = NapObganismConverter.ResourceToObganism(resource).Trim();

			expected = Regex.Replace(expected.Trim(), @"\s+", " ");
			actual = Regex.Replace(actual.Trim(), @"\s+", " ");

			Assert.AreEqual(expected, actual);
		}

		static TestCaseData TestCase(string subject, Resource resource, string expected) =>
			new TestCaseData(resource, expected).SetName(subject);

		static readonly object[] SingleResourceTestCases = new[] {
			TestCase("Convert an empty resource",
				new Resource { Name = "cat" },
				"cat"
			),
			TestCase("Convert a resource with a single generic",
				new Resource {
					Name = "hateoas",
					GenericTemplates = new[] { new TemplateType { Name = "T" } }
				},
				"hateoas of T"
			),
			TestCase("Convert a resource with several generics",
				new Resource {
					Name = "hateoas",
					GenericTemplates = new[] {
						new TemplateType { Name = "data" },
						new TemplateType { Name = "error" }
					}
				},
				"hateoas of (data, error)"
			),
			TestCase("Convert a resource with simple fields",
				new Resource {
					Name = "sample",
					Fields = new Dictionary<string, FieldType> {
						["b"] = new BoolType(),
						["i"] = new IntType(),
						["f"] = new FloatType(),
						["c"] = new CharType(),
						["s"] = new StringType(),
						["d"] = new DateType(),
						["t"] = new DatetimeType(),
						["u"] = new DurationType(),

						["e"] = new SetType(new IntType()),
						["l"] = new ListType(new CharType()),
						["m"] = new MapType(new StringType(), new ResourceType {
							Name = "tuple",
							Generics = new FieldType[] {
								new DateType(),
								new DurationType()
							}
						}),

						["p"] = new TemplateType { Name = "T" },
						["r"] = new ResourceType { Name = "R" }
					}
				},
				@"
					sample {
						b : bool
						i : int
						f : float
						c : char
						s : string
						d : date
						t : datetime
						u : duration

						e : set of int
						l : list of char
						m : map of (string, tuple of (date, duration))

						p : T
						r : R
					}
				"
			),
			TestCase("Convert a resource with fields and meta",
				new Resource {
					Name = "sample",
					Fields = new Dictionary<string, FieldType> {
						["b"] = new BoolType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Default] = true,
								[NapBuiltInMeta.IsOptional] = true
							}
						},

						["ia"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Default] = 12,
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["ib"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["ic"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MinIsInclusive] = false,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["id"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = false
							}
						},
						["ie"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["if"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = true
							}
						},
						["ig"] = new IntType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},

						["c"] = new CharType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.AllowedValues] = new[] { 'A', 'B', 'C' },
								[NapBuiltInMeta.Default] = 'A'
							}
						},
						["s"] = new StringType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.ForbiddenValues] = new[] { "Two", "Three" },
								[NapBuiltInMeta.Default] = "One"
							}
						},
						["d"] = new DateType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Default] = new DateTime(2020, 01, 01),
							}
						},
						["t"] = new DatetimeType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Default] = new DateTime(2020, 01, 01, 12, 30, 0),
							}
						},
						["u"] = new DurationType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.Default] = new TimeSpan(0, 1, 30),
							}
						},

						["ea"] = new SetType(new IntType()),
						["eb"] = new SetType(new IntType()) {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.AllowDuplicates] = true
							}
						},
						["la"] = new ListType(new CharType()),
						["lb"] = new ListType(new CharType()) {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.AllowDuplicates] = false
							}
						},
						["m"] = new MapType(new StringType(), new ResourceType {
							Name = "tuple",
							Generics = new FieldType[] {
								new DateType(),
								new DurationType {
									Meta = new Dictionary<string, object?> {
										[NapBuiltInMeta.Min] = new TimeSpan(24, 0, 0),
										[NapBuiltInMeta.MinIsInclusive] = false
									}
								}
							}
						}),

						["email"] = new StringType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.AllowEmpty] = false,
								[NapBuiltInMeta.AllowMultiline] = false,
								[NapBuiltInMeta.Pattern] = new Regex(@"[a-z0-9._-]@[a-z0-9._-]+\.[a-z]+", RegexOptions.IgnoreCase)
							}
						},
						["password"] = new StringType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.NotSameAs] = "email"
							}
						},
						["password confirmation"] = new StringType {
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.SameAs] = "password"
							}
						},

						["ra"] = new ResourceType {
							Name = "sample",
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.SelfReference] = SelfReference.Enforce
							}
						},
						["rb"] = new ResourceType {
							Name = "sample",
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
							}
						},
						["rc"] = new ResourceType {
							Name = "sample",
							Meta = new Dictionary<string, object?> {
								[NapBuiltInMeta.SelfReference] = SelfReference.Allow
							}
						}
					}
				},
				// Note that nested generics are silently ignored.
				@"
					sample {
						b : bool -- ( default(true), optional )

						ia : int -- ( default(12), in(1, 42) )
						ib : int -- between(1, included, 42, excluded)
						ic : int -- between(1, 42)
						id : int -- above(1)
						ie : int -- below(42)
						if : int -- min(1)
						ig : int -- max(42)

						c : char -- ( one of(""A"", ""B"", ""C""), default(""A"") )
						s : string -- ( not one of(""Two"", ""Three""), default(""One"") )
						d : date -- default(""2020-01-01"")
						t : datetime -- default(""2020-01-01 12:30:00"")
						u : duration -- default(""1m30s"")

						ea : set of int
						eb : set of int -- allow duplicates

						la : list of char
						lb : list of char -- no duplicates

						m : map of (string, tuple of (date, duration))

						email : string -- ( not empty, not multiline, pattern(""[a-z0-9._-]@[a-z0-9._-]+\.[a-z]+"", ignore case) )
						password : string -- not same as(""email"")
						password confirmation : string -- same as(""password"")

						ra : sample -- self
						rb : sample -- not self
						rc : sample
					}
				"

				// TODO: meta on collection generics
				// nested generics are tested -- ok
			)
		};
	}
}
