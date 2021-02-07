using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nap.Tests;
using NUnit.Framework;
using Obganism;

namespace Nap.Obganism.Tests {
	static class ObganismNapConverterTests {
		[Test]
		[TestCaseSource(nameof(SingleResourceTestCases))]
		public static void SingleResourceTests(ObganismObject obganismObject, PartialResource expected) {
			var actual = ObganismNapConverter.ObjectToResource(obganismObject);

			NapAssert.AssertResource(expected, actual);
		}

		static TestCaseData HowTo(string subject, string obganismObject, PartialResource expected) =>
			new TestCaseData(ObganismSerializer.Deserialize(obganismObject).Single(), expected).SetName(subject);

		static readonly object[] SingleResourceTestCases = new[] {
			HowTo("Declare an empty resource",
				"cat",
				new PartialResource { Name = "cat" }
			),
			HowTo("Declare primitive fields",
				@"
					cat {
						b : bool
						i : int
						f : float
						c : char
						s : string
						d : date
						t : datetime
						p : duration

						e : set of string
						l : list of date
						m : map of (cat, map of (datetime, duration))
					}
				",
				new PartialResource {
					Name = "cat",
					Fields = {
						["b"] = new PartialFieldType { Name = NapBuiltInTypes.Bool },
						["i"] = new PartialFieldType { Name = NapBuiltInTypes.Int },
						["f"] = new PartialFieldType { Name = NapBuiltInTypes.Float },
						["c"] = new PartialFieldType { Name = NapBuiltInTypes.Char },
						["s"] = new PartialFieldType { Name = NapBuiltInTypes.String },
						["d"] = new PartialFieldType { Name = NapBuiltInTypes.Date },
						["t"] = new PartialFieldType { Name = NapBuiltInTypes.Datetime },
						["p"] = new PartialFieldType { Name = NapBuiltInTypes.Duration },

						["e"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.String }
							}
						},
						["l"] = new PartialFieldType {
							Name = NapBuiltInTypes.List,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.Date }
							}
						},
						["m"] = new PartialFieldType {
							Name = NapBuiltInTypes.Map,
							Generics = {
								[0] = new PartialFieldType { Name = "cat" },
								[1] = new PartialFieldType {
									Name = NapBuiltInTypes.Map,
									Generics = {
										[0] = new PartialFieldType { Name = NapBuiltInTypes.Datetime },
										[1] = new PartialFieldType { Name = NapBuiltInTypes.Duration }
									}
								}
							}
						}
					}
				}
			),
			HowTo("Declare a resource with generic templates",
				"tuple of (A, B, C of D)",
				// Generic generic templates are valid in Obganism (though a bit useless, I must acknowledge),
				// but are ignored when converting to Nap resources.
				new PartialResource {
					Name = "tuple",
					GenericTemplates = {
						[0] = "A",
						[1] = "B",
						[2] = "C"
					}
				}
			),
			HowTo("Set built-in meta on field types",
				@"
					sample of T {
						b : bool -- ( default(true), optional )
						c : char -- ( enum(""A"", ""B"", ""C""), default(""A"") )

						ia : int -- ( min(1), max(42) )
						ib : int -- ( min(1, inclusive), max(42, exclusive) )
						ic : int -- ( at least(1), at most(42) )
						id : int -- ( not below(1), not above(42) )
						ie : int -- ( above(1), below(42) )

						fa : float -- between(1, 42)
						fb : float -- between(1, included, 42, included)
						fc : float -- in(1, 42)
						fd : float -- in(1, 42, excluded)
						fe : float -- in(1, excluded, 42)

						d : date -- ( not before(""2020-12-31""), not after(""2021-01-31"") )
						t : datetime -- ( after(""2021-01-01 12:00:00"", included), before(""2021-01-01 13:00:00"") )
						ua : duration -- in(""3h 30min"", ""P4DT12H30M5S"")
						ub : duration -- in(""1 minute 59s"", ""10 days 1 minute 30s"")

						sa : string -- enum(""A"", ""B"", ""C"")
						sb : string -- one of(""A"", ""B"", ""C"")
						sc : string -- amongst(""A"", ""B"", ""C"")
						sd : string -- not one of(""A"", ""B"", ""C"")
						se : string -- not amongst(""A"", ""B"", ""C"")

						address : string -- ( not empty, multiline )
						email : string -- pattern(""[a-z0-9._-]+@[a-z0-9]+.[a-z0-9]+"", ignore case)
						email confirmation : string -- same as(email)

						x : T
						y : T -- not same as(x)

						cycle : sample of T -- self
						other : sample of T -- not self

						s : set of T -- allow duplicates
						l : list of T -- no duplicates
					}
				",
				new PartialResource {
					Name = "sample",
					GenericTemplates = {
						[0] = "T"
					},
					Fields = {
						["b"] = new PartialFieldType {
							Name = NapBuiltInTypes.Bool,
							Meta = {
								[NapBuiltInMeta.Default] = true,
								[NapBuiltInMeta.IsOptional] = true
							}
						},
						["c"] = new PartialFieldType {
							Name = NapBuiltInTypes.Char,
							Meta = {
								[NapBuiltInMeta.AllowedValues] = new[] { 'A', 'B', 'C' },
								[NapBuiltInMeta.Default] = 'A',
							}
						},

						["ia"] = new PartialFieldType {
							Name = NapBuiltInTypes.Int,
							Meta = {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["ib"] = new PartialFieldType {
							Name = NapBuiltInTypes.Int,
							Meta = {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["ic"] = new PartialFieldType {
							Name = NapBuiltInTypes.Int,
							Meta = {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["id"] = new PartialFieldType {
							Name = NapBuiltInTypes.Int,
							Meta = {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["ie"] = new PartialFieldType {
							Name = NapBuiltInTypes.Int,
							Meta = {
								[NapBuiltInMeta.Min] = 1,
								[NapBuiltInMeta.MinIsInclusive] = false,
								[NapBuiltInMeta.Max] = 42,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},

						["fa"] = new PartialFieldType {
							Name = NapBuiltInTypes.Float,
							Meta = {
								[NapBuiltInMeta.Min] = 1f,
								[NapBuiltInMeta.MinIsInclusive] = false,
								[NapBuiltInMeta.Max] = 42f,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["fb"] = new PartialFieldType {
							Name = NapBuiltInTypes.Float,
							Meta = {
								[NapBuiltInMeta.Min] = 1f,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42f,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["fc"] = new PartialFieldType {
							Name = NapBuiltInTypes.Float,
							Meta = {
								[NapBuiltInMeta.Min] = 1f,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42f,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["fd"] = new PartialFieldType {
							Name = NapBuiltInTypes.Float,
							Meta = {
								[NapBuiltInMeta.Min] = 1f,
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = 42f,
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["fe"] = new PartialFieldType {
							Name = NapBuiltInTypes.Float,
							Meta = {
								[NapBuiltInMeta.Min] = 1f,
								[NapBuiltInMeta.MinIsInclusive] = false,
								[NapBuiltInMeta.Max] = 42f,
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},

						["d"] = new PartialFieldType {
							Name = NapBuiltInTypes.Date,
							Meta = {
								[NapBuiltInMeta.Min] = new DateTime(2020, 12, 31),
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = new DateTime(2021, 1, 31),
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["t"] = new PartialFieldType {
							Name = NapBuiltInTypes.Datetime,
							Meta = {
								[NapBuiltInMeta.Min] = new DateTime(2021, 1, 1, 12, 0, 0),
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = new DateTime(2021, 1, 1, 13, 0, 0),
								[NapBuiltInMeta.MaxIsInclusive] = false
							}
						},
						["ua"] = new PartialFieldType {
							Name = NapBuiltInTypes.Duration,
							Meta = {
								[NapBuiltInMeta.Min] = new TimeSpan(0, 3, 30, 0),
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = new TimeSpan(4, 12, 30, 5),
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},
						["ub"] = new PartialFieldType {
							Name = NapBuiltInTypes.Duration,
							Meta = {
								[NapBuiltInMeta.Min] = new TimeSpan(0, 0, 1, 59),
								[NapBuiltInMeta.MinIsInclusive] = true,
								[NapBuiltInMeta.Max] = new TimeSpan(10, 0, 1, 30),
								[NapBuiltInMeta.MaxIsInclusive] = true
							}
						},

						["sa"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.AllowedValues] = new[] { "A", "B", "C" }
							}
						},
						["sb"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.AllowedValues] = new[] { "A", "B", "C" }
							}
						},
						["sc"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.AllowedValues] = new[] { "A", "B", "C" }
							}
						},
						["sd"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.ForbiddenValues] = new[] { "A", "B", "C" }
							}
						},
						["se"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.ForbiddenValues] = new[] { "A", "B", "C" }
							}
						},

						["address"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.AllowEmpty] = false,
								[NapBuiltInMeta.AllowMultiline] = true
							}
						},
						["email"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.Pattern] = new Regex("[a-z0-9._-]+@[a-z0-9]+.[a-z0-9]+", RegexOptions.IgnoreCase)
							}
						},
						["email confirmation"] = new PartialFieldType {
							Name = NapBuiltInTypes.String,
							Meta = {
								[NapBuiltInMeta.SameAs] = "email"
							}
						},

						["x"] = new PartialFieldType { Name = "T" },
						["y"] = new PartialFieldType {
							Name = "T",
							Meta = {
								[NapBuiltInMeta.NotSameAs] = "x"
							}
						},

						["cycle"] = new PartialFieldType {
							Name = "sample",
							Generics = {
								[0] = new PartialFieldType { Name = "T" }
							},
							Meta = {
								[NapBuiltInMeta.SelfReference] = SelfReference.Enforce
							}
						},
						["other"] = new PartialFieldType {
							Name = "sample",
							Generics = {
								[0] = new PartialFieldType { Name = "T" }
							},
							Meta = {
								[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
							}
						},

						["s"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType { Name = "T" }
							},
							Meta = {
								[NapBuiltInMeta.AllowDuplicates] = true
							}
						},
						["l"] = new PartialFieldType {
							Name = NapBuiltInTypes.List,
							Generics = {
								[0] = new PartialFieldType { Name = "T" }
							},
							Meta = {
								[NapBuiltInMeta.AllowDuplicates] = false
							}
						}
					}
				}
			),
			HowTo("Set built-in meta on field collection type generics",
				@"
					sample {
						sa : set of int -- ( all min(1), all max(42) )
						sb : set of int -- ( all at least(1), all at most(42) )
						sc : set of int -- ( all above(1), all below(42) )
						sd : set of int -- ( all after(1), all before(42) )
						se : set of int -- ( not any below(1), not any above(42) )
						sf : set of int -- ( not any before(1), not any after(42) )

						la : list of float -- all between(1, 42)
						lb : list of float -- all in(1, 42)

						sg : set of char -- all one of(""A"", ""B"")
						sh : set of char -- all amongst(""A"", ""B"")
						si : set of char -- not any one of(""A"", ""B"")
						sj : set of char -- not any amongst(""A"", ""B"")

						addresses : set of string -- ( all multiline, any empty, all optional )
						codes : set of string -- ( not any multiline, not any empty, not any optional, all pattern(""\w{2}\d-\d{2}\w"") )

						ma : map of(char, sample) -- all self
						mb : map of(char, sample) -- not any self

						mc : map of(char, int) -- all same as(another int field)
						md : map of(char, int) -- not any same as(another int field)
					}
				",
				new PartialResource {
					Name = "sample",
					Fields = {
						["sa"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								}
							}
						},
						["sb"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								}
							}
						},
						["sc"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = false,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = false
									}
								}
							}
						},
						["sd"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = false,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = false
									}
								}
							}
						},
						["se"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								}
							}
						},
						["sf"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 42,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								}
							}
						},

						["la"] = new PartialFieldType {
							Name = NapBuiltInTypes.List,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Float,
									Meta = {
										[NapBuiltInMeta.Min] = 1f,
										[NapBuiltInMeta.MinIsInclusive] = false,
										[NapBuiltInMeta.Max] = 42f,
										[NapBuiltInMeta.MaxIsInclusive] = false
									}
								}
							}
						},
						["lb"] = new PartialFieldType {
							Name = NapBuiltInTypes.List,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Float,
									Meta = {
										[NapBuiltInMeta.Min] = 1f,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 42f,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								}
							}
						},

						["sg"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Char,
									Meta = {
										[NapBuiltInMeta.AllowedValues] = new[] { 'A', 'B' }
									}
								}
							}
						},
						["sh"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Char,
									Meta = {
										[NapBuiltInMeta.AllowedValues] = new[] { 'A', 'B' }
									}
								}
							}
						},
						["si"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Char,
									Meta = {
										[NapBuiltInMeta.ForbiddenValues] = new[] { 'A', 'B' }
									}
								}
							}
						},
						["sj"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.Char,
									Meta = {
										[NapBuiltInMeta.ForbiddenValues] = new[] { 'A', 'B' }
									}
								}
							}
						},

						["addresses"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.String,
									Meta = {
										[NapBuiltInMeta.AllowMultiline] = true,
										[NapBuiltInMeta.AllowEmpty] = true,
										[NapBuiltInMeta.IsOptional] = true
									}
								}
							}
						},
						["codes"] = new PartialFieldType {
							Name = NapBuiltInTypes.Set,
							Generics = {
								[0] = new PartialFieldType {
									Name = NapBuiltInTypes.String,
									Meta = {
										[NapBuiltInMeta.AllowMultiline] = false,
										[NapBuiltInMeta.AllowEmpty] = false,
										[NapBuiltInMeta.IsOptional] = false,
										[NapBuiltInMeta.Pattern] = new Regex(@"\w{2}\d-\d{2}\w")
									}
								}
							}
						},

						["ma"] = new PartialFieldType {
							Name = NapBuiltInTypes.Map,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.Char },
								[1] = new PartialFieldType {
									Name = "sample",
									Meta = {
										[NapBuiltInMeta.SelfReference] = SelfReference.Enforce
									}
								}
							}
						},
						["mb"] = new PartialFieldType {
							Name = NapBuiltInTypes.Map,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.Char },
								[1] = new PartialFieldType {
									Name = "sample",
									Meta = {
										[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
									}
								}
							}
						},
						["mc"] = new PartialFieldType {
							Name = NapBuiltInTypes.Map,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.Char },
								[1] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.SameAs] = "another int field"
									}
								}
							}
						},
						["md"] = new PartialFieldType {
							Name = NapBuiltInTypes.Map,
							Generics = {
								[0] = new PartialFieldType { Name = NapBuiltInTypes.Char },
								[1] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.NotSameAs] = "another int field"
									}
								}
							}
						}
					}
				}
			)

			/*

			TODO:

			- Set built-in meta on field custom type generics
				A. 1st generic
				B. Not allowed

			- Set extra meta on field type
				* No parameter
				* 1 parameter
				* Several parameters of the same type
				* Several parameters of different types

			- Set extra meta on field type generics
				* On collection's generics
				* On custom type's generics

			*/
		};

		// ----

		[Test]
		[TestCaseSource(nameof(ContainerTestCases))]
		public static void ContainerTests(IReadOnlyList<ObganismObject> obganismObjects, PartialContainer expected) {
			var actual = ObganismNapConverter.ObjectsToContainer(obganismObjects);

			NapAssert.AssertContainer(expected, actual);
		}

		static TestCaseData HowTo(string subject, string obganismObjects, PartialContainer expected) =>
			new TestCaseData(ObganismSerializer.Deserialize(obganismObjects), expected).SetName(subject);

		static TestCaseData HowTo(string subject, IReadOnlyList<ObganismObject> obganismObjects, PartialContainer expected) =>
			new TestCaseData(obganismObjects, expected).SetName(subject);

		static readonly object[] ContainerTestCases = new[] {
			HowTo("Declare an empty container",
				"",
				new PartialContainer()
			),
			HowTo("Declare several empty resources",
				"cat, dog, camel",
				new PartialContainer {
					Resources = {
						new PartialResource { Name = "cat" },
						new PartialResource { Name = "dog" },
						new PartialResource { Name = "camel" }
					}
				}
			),
			HowTo("Declare a container with resources",
				@"
					camel {
						id : int
						name :string
						address : address
					}

					dog {
						id : int
						name :string
						bark power : float
						is good : bool
						best friend : dog
						friends : set of dog
					}

					cat {
						id : int -- min(1)
						name : string -- in(1, 30)
						purr power : float -- in(0.0, 1.0)
						is cute : bool -- default(true)
						best friend : cat -- ( not self, optional )
						friends : set of cat -- ( not any self, allow empty )
					}
				",
				new PartialContainer {
					Resources = {
						new PartialResource {
							Name = "camel",
							Fields = {
								["id"] = new PartialFieldType { Name = NapBuiltInTypes.Int },
								["name"] = new PartialFieldType { Name = NapBuiltInTypes.String },
								["address"] = new PartialFieldType { Name = "address" },
							}
						},
						new PartialResource {
							Name = "dog",
							Fields = {
								["id"] = new PartialFieldType { Name = NapBuiltInTypes.Int },
								["name"] = new PartialFieldType { Name = NapBuiltInTypes.String },
								["bark power"] = new PartialFieldType { Name = NapBuiltInTypes.Float },
								["is good"] = new PartialFieldType { Name = NapBuiltInTypes.Bool },
								["best friend"] = new PartialFieldType { Name = "dog" },
								["friends"] = new PartialFieldType {
									Name = NapBuiltInTypes.Set,
									Generics = {
										[0] = new PartialFieldType { Name = "dog" }
									}
								}
							}
						},
						new PartialResource {
							Name = "cat",
							Fields = {
								["id"] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true
									}
								},
								["name"] = new PartialFieldType {
									Name = NapBuiltInTypes.String,
									Meta = {
										[NapBuiltInMeta.Min] = 1,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 30,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								},
								["purr power"] = new PartialFieldType {
									Name = NapBuiltInTypes.Float,
									Meta = {
										[NapBuiltInMeta.Min] = 0f,
										[NapBuiltInMeta.MinIsInclusive] = true,
										[NapBuiltInMeta.Max] = 1f,
										[NapBuiltInMeta.MaxIsInclusive] = true
									}
								},
								["is cute"] = new PartialFieldType {
									Name = NapBuiltInTypes.Bool,
									Meta = {
										[NapBuiltInMeta.Default] = true
									}
								},
								["best friend"] = new PartialFieldType {
									Name = "cat",
									Meta = {
										[NapBuiltInMeta.SelfReference] = SelfReference.Forbid,
										[NapBuiltInMeta.IsOptional] = true
									}
								},
								["friends"] = new PartialFieldType {
									Name = NapBuiltInTypes.Set,
									Generics = {
										[0] = new PartialFieldType {
											Name = "cat",
											Meta = {
												[NapBuiltInMeta.SelfReference] = SelfReference.Forbid
											}
										}
									},
									Meta = {
										[NapBuiltInMeta.AllowEmpty] = true
									}
								}
							}
						}
					}
				}
			),
			HowTo("Declare a container with generic resources",
				@"
					cat

					api response {
						cats : hateoas of cat
					}

					hateoas of T {
						count : int -- min(0)
						data : list of T
					}
				",
				new PartialContainer {
					Resources = {
						new PartialResource { Name = "cat" },
						new PartialResource {
							Name = "api response",
							Fields = {
								["cats"] = new PartialFieldType {
									Name = "hateoas",
									Generics = {
										[0] = new PartialFieldType { Name = "cat" }
									}
								}
							}
						},
						new PartialResource {
							Name = "hateoas",
							GenericTemplates = {
								[0] = "T"
							},
							Fields = {
								["count"] = new PartialFieldType {
									Name = NapBuiltInTypes.Int,
									Meta = {
										[NapBuiltInMeta.Min] = 0,
										[NapBuiltInMeta.MinIsInclusive] = true
									}
								},
								["data"] = new PartialFieldType {
									Name = NapBuiltInTypes.List,
									Generics = {
										[0] = new PartialFieldType {
											Name = "T"
										}
									}
								}
							}
						}
					}
				}
			),
			HowTo("Configure a container with a first unnamed object",
				new[] {
					new ObganismObject {
						Type = new ObganismType {
							Name = string.Empty,
							Generics = Array.Empty<ObganismType>()
						},
						Properties = Array.Empty<ObganismProperty>(),
						Modifiers = new[] {
							new ObganismModifier {
								Name = "name",
								Parameters = new[] {
									new ObganismModifierParameter.Name { Value = "a container has no name" }
								}
							}
						}
					},
					new ObganismObject {
						Type = new ObganismType {
							Name = "cat",
							Generics = Array.Empty<ObganismType>()
						},
						Properties = Array.Empty<ObganismProperty>(),
						Modifiers = Array.Empty<ObganismModifier>()
					}
				},
				new PartialContainer {
					Name = "a container has no name",
					Resources = {
						new PartialResource { Name = "cat" }
					}
				}
			)
		};

		// ----

		[Test]
		[TestCaseSource(nameof(ContainerFromSourceTestCases))]
		public static void ContainerFromSourceTests(string obganismObjects, PartialContainer expected) {
			var actual = ObganismNapConverter.ObjectsToContainer(obganismObjects);

			NapAssert.AssertContainer(expected, actual);
		}

		static TestCaseData HowToFromSource(string subject, string obganismObjects, PartialContainer expected) =>
			new TestCaseData(obganismObjects, expected).SetName(subject);

		static readonly object[] ContainerFromSourceTestCases = new[] {
			HowToFromSource("Declare a container without configuration",
				"cat",
				new PartialContainer {
					Resources = {
						new PartialResource { Name = "cat" }
					}
				}
			),
			HowToFromSource("Configure a container with a loose modifier list",
				@"
					-- (
						name(a container has no name)
					)

					cat
				",
				new PartialContainer {
					Name = "a container has no name",
					Resources = {
						new PartialResource { Name = "cat" }
					}
				}
			)
		};
	}
}
