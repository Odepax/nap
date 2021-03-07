using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Builders;

namespace Nap {
	static class DomainTests {
		[Test]
		public static void Unordered_bounds_throw() {
			Assert.Throws<ArgumentException>(() => new Domain<int>(42, false, 1, false));
		}

		[Test]
		public static void Exclusive_single_throws() {
			Assert.Throws<ArgumentException>(() => new Domain<int>(2, false, 2, false));
			Assert.Throws<ArgumentException>(() => new Domain<int>(2, true, 2, false));
			Assert.Throws<ArgumentException>(() => new Domain<int>(2, false, 2, true));
		}

		[Test]
		public static void Default_equals_default() {
			AssertDomain<int>(default, default);
		}

		[Test]
		public static void Default_doesnt_contain_any_value() {
			var Df = default(Domain<int>);

			Assert.IsTrue(Df.DoesNotContain(-12));
			Assert.IsTrue(Df.DoesNotContain(0));
			Assert.IsTrue(Df.DoesNotContain(42));
		}

		[Test]
		public static void Containement_in_single() {
			var Df = new Domain<int>(0, true, 0, true);

			Assert.IsTrue(Df.DoesNotContain(-1));
			Assert.IsTrue(Df.Contains(0));
			Assert.IsTrue(Df.DoesNotContain(1));
		}

		[Test]
		public static void Containement_in_inclusive_range() {
			var Df = new Domain<int>(-10, true, 10, true);

			Assert.IsTrue(Df.DoesNotContain(-12));
			Assert.IsTrue(Df.Contains(-10));
			Assert.IsTrue(Df.Contains(-8));
			Assert.IsTrue(Df.Contains(0));
			Assert.IsTrue(Df.Contains(9));
			Assert.IsTrue(Df.Contains(10));
			Assert.IsTrue(Df.DoesNotContain(42));
		}

		[Test]
		public static void Containement_in_exclusive_range() {
			var Df = new Domain<int>(-10, false, 10, false);

			Assert.IsTrue(Df.DoesNotContain(-12));
			Assert.IsTrue(Df.DoesNotContain(-10));
			Assert.IsTrue(Df.Contains(-8));
			Assert.IsTrue(Df.Contains(0));
			Assert.IsTrue(Df.Contains(9));
			Assert.IsTrue(Df.DoesNotContain(10));
			Assert.IsTrue(Df.DoesNotContain(42));
		}

		[Test]
		public static void Containement_in_range() {
			Assert.IsTrue(new Domain<int>(-10, true, 10, true).Contains(-10));
			Assert.IsTrue(new Domain<int>(-10, false, 10, true).Contains(10));
			Assert.IsTrue(new Domain<int>(-10, true, 10, false).DoesNotContain(10));
			Assert.IsTrue(new Domain<int>(-10, false, 10, false).DoesNotContain(-10));

			Assert.IsTrue(new Domain<int>(1, false, 2, true).DoesNotContain(0));
			Assert.IsTrue(new Domain<int>(1, false, 2, true).DoesNotContain(1));
			Assert.IsTrue(new Domain<int>(1, false, 2, true).Contains(2));
			Assert.IsTrue(new Domain<int>(1, false, 2, true).DoesNotContain(3));

			Assert.IsTrue(new Domain<int>(1, true, 2, false).DoesNotContain(0));
			Assert.IsTrue(new Domain<int>(1, true, 2, false).Contains(1));
			Assert.IsTrue(new Domain<int>(1, true, 2, false).DoesNotContain(2));
			Assert.IsTrue(new Domain<int>(1, true, 2, false).DoesNotContain(3));
		}

		[Test]
		[Combinatorial]
		public static void Containement_in_union([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d, [Values] bool e, [Values] bool f) {
			var Df = (
				  new Domain<int>(10, a, 20, b)
				| new Domain<int>(30, c, 40, d)
				| new Domain<int>(40, e, 50, f)
			);

			//  5...10...15...20...25...30...35...40...45...50...55
			//       |_________|         |_________|_________|
			//       a         b         c        de         f

			Assert.IsTrue(Df.DoesNotContain(5));
			Assert.IsTrue(Df.Contains(10) == a);
			Assert.IsTrue(Df.Contains(15));
			Assert.IsTrue(Df.Contains(20) == b);
			Assert.IsTrue(Df.DoesNotContain(25));
			Assert.IsTrue(Df.Contains(30) == c);
			Assert.IsTrue(Df.Contains(35));
			Assert.IsTrue(Df.Contains(40) == (d || e));
			Assert.IsTrue(Df.Contains(45));
			Assert.IsTrue(Df.Contains(50) == f);
			Assert.IsTrue(Df.DoesNotContain(55));
		}

		// Assertions
		// ----

		static void AssertContains<T>(Domain<T> actual, params (T Value, bool IsContained)[] valuesForContainmentTesting) where T : IComparable<T> {
			foreach (var (value, isContained) in valuesForContainmentTesting)
				if (isContained)
					Assert.IsTrue(actual.Contains(value), "Domain should contain value " + value.ToString());
				else
					Assert.IsFalse(actual.Contains(value), "Domain should not contain value " + value.ToString());
		}

		static void AssertDomain<T>(Domain<T> expected, Domain<T> actual, params T[] valuesForContainmentTesting) where T : IComparable<T> {
			Assert.IsTrue(expected.Equals(actual));
			Assert.IsTrue(expected == actual);
			Assert.IsFalse(expected != actual);
			Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());

			foreach (var value in valuesForContainmentTesting)
				Assert.AreEqual(expected.Contains(value), actual.Contains(value));
		}

		static void AssertNotDomain<T>(Domain<T> expected, Domain<T> actual) where T : IComparable<T> {
			Assert.IsFalse(expected.Equals(actual));
			Assert.IsFalse(expected == actual);
			Assert.IsTrue(expected != actual);
			Assert.AreNotEqual(expected.GetHashCode(), actual.GetHashCode());
		}

		delegate Domain<T> Operation<T>(Domain<T> a, Domain<T> b) where T : IComparable<T>;

		/// <summary> Identity (of whatever $ operation): A $ default == A </summary>
		static void AssertIdentity<T>(Operation<T> op, Domain<T> a, params T[] valuesForContainmentTesting) where T : IComparable<T> =>
			AssertDomain(op(a, default), a, valuesForContainmentTesting);

		/// <summary> Commutativity (of whatever $ operation): A $ B == B $ A </summary>
		static void AssertCommutativity<T>(Operation<T> op, Domain<T> a, Domain<T> b, params T[] valuesForContainmentTesting) where T : IComparable<T> =>
			AssertDomain(op(a, b), op(b, a), valuesForContainmentTesting);

		/// <summary> Associativity (of whatever $ operation): (A $ B) $ C == A $ (B $ C) </summary>
		static void AssertAssociativity<T>(Operation<T> op, Domain<T> a, Domain<T> b, Domain<T> c, params T[] valuesForContainmentTesting) where T : IComparable<T> =>
			AssertDomain(op(op(a, b), c), op(a, op(b, c)), valuesForContainmentTesting);

		/// <summary> Distributivity (of whatever $ operation over whatever ~ other operation): A $ (B ~ C) = (A $ B) ~ (A $ C) </summary>
		static void AssertDistributivity<T>(Operation<T> op, Operation<T> otherOp, Domain<T> a, Domain<T> b, Domain<T> c, params T[] valuesForContainmentTesting) where T : IComparable<T> =>
			AssertDomain(op(a, otherOp(b, c)), otherOp(op(a, b), op(a, c)), valuesForContainmentTesting);

		// Union
		// ----

		readonly static Operation<int> Union = (a, b) => a | b;

		[Test]
		public static void Union_with_default() {
			var A = new Domain<int>(10, true, 20, true);
			var B = default(Domain<int>);

			var testValues = new[] { 5, 10, 25, 20, 25 };
			//                           |___A___|

			AssertDomain(A, A | B);
			AssertCommutativity(Union, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Union_with_other([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(30, c, 40, d);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45 };
			//                           |___A___|       |___B___|
			//                           a       b       c       d

			AssertContains(A | B,
				(5, false),
				(10, a),
				(15, true),
				(20, b),
				(25, false),
				(30, c),
				(35, true),
				(40, d),
				(45, false)
			);

			AssertCommutativity(Union, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Union_with_touching([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(20, c, 30, d);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |___A___|___B___|
			//                           a      bc       d

			AssertContains(A | B,
				(5, false),
				(10, a),
				(15, true),
				(20, b || c),
				(25, true),
				(30, d),
				(35, false)
			);

			AssertCommutativity(Union, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Union_with_overlap([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 25, b);
			var B = new Domain<int>(20, c, 35, d);

			//                                   c_____B_____d
			//                                   |           |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40 };
			//                           |_____A____|
			//                           a          b

			AssertDomain(new Domain<int>(10, a, 35, d), A | B, testValues);
			AssertCommutativity(Union, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Union_with_sub([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 30, b);
			var B = new Domain<int>(15, c, 25, d);

			//                               c___B___d
			//                               |       |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |_______A_______|
			//                           a               b

			AssertDomain(A, A | B, testValues);
			AssertCommutativity(Union, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Union_with_equal([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(10, c, 20, d);

			//                           c___B___d
			//                           |       |
			var testValues = new[] { 5, 10, 25, 20, 25 };
			//                           |___A___|
			//                           a       b

			AssertDomain(new Domain<int>(10, a || c, 20, b || d), A | B, testValues);
			AssertCommutativity(Union, A, B, testValues);
		}

		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
		class LowUpCombinatorialAttribute : CombiningStrategyAttribute {
			public LowUpCombinatorialAttribute(): base(new LowUpCombiningStrategy(), new ParameterDataSourceProvider()) {}

			class LowUpCombiningStrategy : ICombiningStrategy {
				public IEnumerable<ITestCaseData> GetTestCases(IEnumerable[] sources) {
					foreach (var lower in sources[0])
					foreach (var upper in sources[1]) if ((int) lower <= (int) upper)
					foreach (var a in sources[2])
					foreach (var b in sources[3])
					foreach (var c in sources[4])
					foreach (var d in sources[5])
						if ((int) lower == (int) upper)
							yield return new TestCaseData(lower, upper, a, b, c, d, true, true);
						else
							foreach (var e in sources[5])
							foreach (var f in sources[5])
								yield return new TestCaseData(lower, upper, a, b, c, d, e, f);
				}
			}
		}

		[Test]
		[LowUpCombinatorial]
		public static void Union_3_and_more(
			[Values(5, 15, 20, 30, 35, 45, 50, 60, 65)] int C_low,
			[Values(10, 15, 25, 30, 40, 45, 55, 60, 70)] int C_up,
			[Values] bool a,
			[Values] bool b,
			[Values] bool c,
			[Values] bool d,
			[Values] bool e,
			[Values] bool f
		) {
			var A = new Domain<int>(15, a, 30, b);
			var B = new Domain<int>(45, c, 60, d);
			var C = new Domain<int>(C_low, e, C_up, f);

			var testValues = new[]{ 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70 };

			//         e_________C_________f
			//         |                   |
			// C_low : v         v    v    |    v    v         v    v         v    v
			// C_up  :      v    v         v    v         v    v         v    v         v
			//         5...10...15...20...25...30...35...40...45...50...55...60...65...70
			//                   |______A______|               |______B______|
			//                   a             b               c             d
			//
			// Merging table:
			//
			//         | C_low
			//         |    5 |  15  |   20 |   30 |   35 |   45 |   50 |   60 |   65 |
			// --------+------+------+------+------+------+------+------+------+------+
			// C_up 10 | /    |      '      '      '      '      '      '      '      '
			//      15 | A?   | A    |      '      '      '      '      '      '      '
			//      25 | A    | A    | A    |      '      '      '      '      '      '
			//      30 | A    | A    | A    | A    |      '      '      '      '      '
			//      40 | A    | A    | A    | A?   | /    |      '      '      '      '
			//      45 | A B? | A B? | A B? | A?B? |   B? |   B  |      '      '      '
			//      55 | A B  | A B  | A B  | A?B  |   B  |   B  |   B  |      '      '
			//      60 | A B  | A B  | A B  | A?B  |   B  |   B  |   B  |   B  |      '
			//      70 | A B  | A B  | A B  | A?B  |   B  |   B  |   B  |   B? | /    |

			// TODO: The abominable test.
			/*
			var mergesWithA = (
				   (C_low <= 20 && 25 <= C_up)
				|| (C_low == 30 && (e || b))
				|| (C_up == 15 && (f || a))
			);

			var mergesWithB = (
				   (C_low <= 50 && 55 <= C_up)
				|| (C_low == 60 && (e || d))
				|| (C_up == 45 && (f || c))
			);

			var expected = default(Domain<int>);

			if (mergesWithA && mergesWithB)
				expected = new Domain<int>(Math.Min(15, C_low), );

			else if (mergesWithA)
				expected = default();

			else if (mergesWithB)
				expected = default();

			if (!mergesWithA) expected |= A;
			if (!mergesWithB) expected |= B;

			AssertDomain(expected, A | B | C);
			*/
			AssertAssociativity(Union, A, B, C, testValues);
		}

		[Test]
		public static void Union_with_non_touching_parts() {
			var A = new Domain<int>(1, true, 2, true);
			var B = new Domain<int>(2, true, 3, true);
			var C = new Domain<int>(3, true, 4, true);

			AssertDomain(new Domain<int>(1, true, 4, true), A | B | C);
			AssertAssociativity(Union, A, B, C);

			A = new Domain<int>(1, true, 2, true);
			B = new Domain<int>(3, true, 4, true);
			C = new Domain<int>(5, true, 6, true);

			// [1..2] and [3..4] are not touching, i.e. there is a "gap" between 2 and 3.
			//
			// Domain<T> only asks for T to be comparable. Here T = int, but Domain<T>
			// isn't supposed to know that there is no value between two other values.
			//
			// All it sees is that 2 and 3 aren't the same value,
			// therefore, the parts are not "touching",
			// therefore, they are not merged...
			AssertNotDomain(new Domain<int>(1, true, 4, true), A | B | C);
			AssertAssociativity(Union, A, B, C);
		}

		[Test]
		public static void Union_all_in_1() {
			var A = (
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			);

			var B = new Domain<int>(1, true, 3, true);
			var C = new Domain<int>(4, true, 6, true);
			var D = new Domain<int>(10, true, 14, true);
			var E = new Domain<int>(15, true, 16, true);
			var F = new Domain<int>(17, true, 19, true);
			var G = new Domain<int>(20, true, 22, true);
			var H = new Domain<int>(23, true, 24, true);

			//                        __B__    __C__              _______D_______     _E_     ___F___     ___G___     _H_
			//                       |     |  |     |            |               |   |   |   |       |   |       |   |   |
			var testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			//                                   [_____]     [______[]__[]___]                   [___________]
			//                                      A           A     A    A                           A

			AssertDomain((
				  new Domain<int>(1, true, 3, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			), A | B);

			AssertDomain((
				  new Domain<int>(4, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			), A | C);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 14, true)
				| new Domain<int>(18, true, 21, true)
			), A | D);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(15, true, 16, true)
				| new Domain<int>(18, true, 21, true)
			), A | E);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(17, true, 21, true)
			), A | F);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 22, true)
			), A | G);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
				| new Domain<int>(23, true, 24, true)
			), A | H);

			AssertDomain((
				  new Domain<int>(1, true, 3, true)
				| new Domain<int>(4, true, 7, true)
				| new Domain<int>(9, true, 14, true)
				| new Domain<int>(15, true, 16, true)
				| new Domain<int>(17, true, 22, true)
				| new Domain<int>(23, true, 24, true)
			), A | B | C | D | E | F | G | H);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(17, true, 22, true)
			), A | (F | G));

			AssertAssociativity(Union, A, C, D, testValues);
			AssertAssociativity(Union, A, C, G, testValues);
			AssertAssociativity(Union, A, F, D, testValues);
			AssertAssociativity(Union, A, F, G, testValues);

			AssertDistributivity(Intersection, Union, A, C, D, testValues);
			AssertDistributivity(Intersection, Union, A, C, G, testValues);
			AssertDistributivity(Intersection, Union, A, F, D, testValues);
			AssertDistributivity(Intersection, Union, A, F, G, testValues);
		}

		// Intersection
		// ----

		readonly static Operation<int> Intersection = (a, b) => a & b;

		[Test]
		[Combinatorial]
		public static void Intersection_with_default() {
			var A = new Domain<int>(10, true, 20, true);
			var B = default(Domain<int>);

			AssertDomain(default, A & B);
			AssertCommutativity(Intersection, A, B);
		}

		[Test]
		[Combinatorial]
		public static void Intersection_with_other([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(30, c, 40, d);

			//  5...10...15...20...25...30...35...40...45
			//       |____A____|         |____B____|
			//       a         b         c         d

			AssertDomain(default, A & B);
			AssertCommutativity(Intersection, A, B);
		}

		[Test]
		[Combinatorial]
		public static void Intersection_with_touching([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(20, c, 30, d);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |___A___|___B___|
			//                           a      bc       d

			if (b && c) AssertDomain(new Domain<int>(20, true, 20, true), A & B);
			else AssertDomain(default, A & B);

			AssertCommutativity(Intersection, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Intersection_with_overlap([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 25, b);
			var B = new Domain<int>(20, c, 35, d);

			//                                   c_____B_____d
			//                                   |           |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40 };
			//                           |_____A____|
			//                           a          b

			AssertDomain(new Domain<int>(20, c, 25, b), A & B);
			AssertCommutativity(Intersection, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Intersection_with_sub([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 30, b);
			var B = new Domain<int>(15, c, 25, d);

			//                               c___B___d
			//                               |       |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |_______A_______|
			//                           a               b

			AssertDomain(B, A & B);
			AssertCommutativity(Intersection, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void Intersection_with_equal([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(10, c, 20, d);

			//                           c___B___d
			//                           |       |
			var testValues = new[] { 5, 10, 25, 20, 25 };
			//                           |___A___|
			//                           a       b

			AssertDomain(new Domain<int>(10, a && c, 20, b && d), A & B);
			AssertCommutativity(Intersection, A, B, testValues);
		}

		[Test]
		[LowUpCombinatorial]
		public static void Intersection_3_and_more(
			[Values(5, 15, 20, 30, 35, 45, 50, 60, 65)] int C_low,
			[Values(10, 15, 25, 30, 40, 45, 55, 60, 70)] int C_up,
			[Values] bool a,
			[Values] bool b,
			[Values] bool c,
			[Values] bool d,
			[Values] bool e,
			[Values] bool f
		) {
			var A = new Domain<int>(15, a, 30, b);
			var B = new Domain<int>(45, c, 60, d);
			var C = new Domain<int>(C_low, e, C_up, f);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70 };

			// TODO: The abominable test. @see Union's abominable test.
			// TODO: Assess that LowUpCombinatorial is as relevant for intersection as it is for union.

			AssertAssociativity(Intersection, A, B, C, testValues);
		}

		[Test]
		public static void Intersection_with_non_touching_parts() {
			var A = new Domain<int>(1, true, 2, true);
			var B = new Domain<int>(2, true, 3, true);
			var C = new Domain<int>(3, true, 4, true);

			AssertDomain(new Domain<int>(2, true, 2, true), A & B);
			AssertCommutativity(Intersection, A, B);

			AssertDomain(default, A & B & C);
			AssertAssociativity(Intersection, A, B, C);

			A = new Domain<int>(1, true, 2, true);
			B = new Domain<int>(3, true, 4, true);
			C = new Domain<int>(5, true, 6, true);

			// [1..2] and [3..4] are not touching, i.e. there is a "gap" between 2 and 3,
			// event if int doesn't define any value "between" integers,
			// IComparable doesn't know about it.
			AssertDomain(default, A & B & C);
			AssertAssociativity(Intersection, A, B, C);
		}

		[Test]
		public static void Intersection_all_in_1() {
			var A = (
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			);

			var B = new Domain<int>(1, true, 3, true);
			var C = new Domain<int>(4, true, 6, true);
			var D = new Domain<int>(10, true, 14, true);
			var E = new Domain<int>(15, true, 16, true);
			var F = new Domain<int>(17, true, 19, true);
			var G = new Domain<int>(20, true, 22, true);
			var H = new Domain<int>(23, true, 24, true);

			//                        __B__    __C__              _______D_______     _E_     ___F___     ___G___     _H_
			//                       |     |  |     |            |               |   |   |   |       |   |       |   |   |
			var testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			//                                   [_____]     [______[]__[]___]                   [___________]
			//                                      A            A    A    A                           A

			AssertDomain(default, A & B);
			AssertDomain(new Domain<int>(5, true, 6, true), A & C);

			AssertDomain((
				  new Domain<int>(10, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
			), A & D);

			AssertDomain(default, A & E);
			AssertDomain(new Domain<int>(18, true, 19, true), A & F);
			AssertDomain(new Domain<int>(20, true, 21, true), A & G);
			AssertDomain(default, A & H);
			AssertDomain(new Domain<int>(20, true, 21, true), A & (B | G));

			AssertDistributivity(Intersection, Union, A, D, G, testValues);
			AssertDistributivity(Intersection, Union, A, F, G, testValues);
			AssertDistributivity(Intersection, Union, A, D, F, testValues);

			B = new Domain<int>(10, true, 18, true);
			C = new Domain<int>(6, true, 13, true) | new Domain<int>(17, true, 19, true) | new Domain<int>(20, true, 22, true);

			//                          ___________C____________                 ___C___     ___C___
			//                         |             _______B___|_______________|___    |   |       |
			//                         |            |           |               |   |   |   |       |
			testValues = new[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
			//                      [_____]     [______[]__[]___]                  [____________]
			//                         A            A     A   A                           A

			AssertDomain((
				  new Domain<int>(10, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 18, true)
			), A & B);

			AssertDomain((
				  new Domain<int>(6, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 19, true)
				| new Domain<int>(20, true, 21, true)
			), A & C);

			AssertDomain((
				  new Domain<int>(10, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 18, true)
			), A & B & C);

			AssertAssociativity(Intersection, A, B, C, testValues);

			AssertDistributivity(Intersection, Union, A, B, C, testValues);
			AssertDistributivity(Intersection, Union, B, C, A, testValues);
			AssertDistributivity(Intersection, Union, C, A, B, testValues);

			A = (
				  new Domain<int>(1, true, 2, true)
				| new Domain<int>(3, true, 4, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(8, true, 10, true)
				| new Domain<int>(12, true, 15, false)
				| new Domain<int>(17, true, 19, true)
				| new Domain<int>(24, false, 27, false)
				| new Domain<int>(29, true, 31, true)
				| new Domain<int>(32, true, 34, true)
			);

			B = (
				  new Domain<int>(6, true, 8, false)
				| new Domain<int>(14, false, 16, true)
				| new Domain<int>(19, true, 20, true)
				| new Domain<int>(21, true, 22, true)
				| new Domain<int>(23, false, 25, true)
				| new Domain<int>(26, false, 33, false)
			);

			//                                   __B__                        ___B___             _B_     _B_     ___B___     _____________B_____________
			//                                  [     [                      ]       ]           [   ]   [   ]   ]       ]   ]                           [
			testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 };
			//                   [__]  [__]  [__A__]  [__A___]       [_____A_____[       [___A___]                   ]_____A_____[       [___A___]   [___A___]

			AssertDomain((
				  new Domain<int>(6, true, 7, true)
				| new Domain<int>(14, false, 15, false)
				| new Domain<int>(19, true, 19, true)
				| new Domain<int>(24, false, 25, true)
				| new Domain<int>(26, false, 27, false)
				| new Domain<int>(29, true, 31, true)
				| new Domain<int>(32, true, 33, false)
			), A & B);

			AssertCommutativity(Intersection, A, B, testValues);
		}

		// Difference
		// ----

		readonly static Operation<int> Difference = (a, b) => a - b;

		[Test]
		[Combinatorial]
		public static void Difference_with_default() {
			var A = new Domain<int>(10, true, 20, true);
			var B = default(Domain<int>);

			AssertDomain(A, A - B);
			AssertDomain(default, B - A);
		}

		[Test]
		[Combinatorial]
		public static void Difference_with_other([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(30, c, 40, d);

			//  5...10...15...20...25...30...35...40...45
			//       |____A____|         |____B____|
			//       a         b         c         d

			AssertDomain(A, A - B);
			AssertDomain(B, B - A);
		}

		[Test]
		[Combinatorial]
		public static void Difference_with_touching([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(20, c, 30, d);

			// 5...10...15...20...25...30...35
			//      |____A____|____B____|
			//      a        bc         d

			AssertDomain(new Domain<int>(10, a, 20, b && !c), A - B);
			AssertDomain(new Domain<int>(20, c && !b, 30, d), B - A);
		}

		[Test]
		[Combinatorial]
		public static void Difference_with_overlap([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 25, b);
			var B = new Domain<int>(20, c, 35, d);

			//                 c______B_______d
			//                 |              |
			//  5...10...15...20...25...30...35...40
			//       |______A_______|
			//       a              b

			AssertDomain(new Domain<int>(10, a, 20, !c), A - B);
			AssertDomain(new Domain<int>(25, !b, 35, d), B - A);
		}

		[Test]
		[Combinatorial]
		public static void Difference_with_sub([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 30, b);
			var B = new Domain<int>(15, c, 25, d);

			//           c____B____d
			//           |         |
			// 5...10...15...20...25...30...35
			//      |_________A_________|
			//      a                   b

			AssertContains(A - B,
				(5, false),
				(10, a),
				(15, !c),
				(20, false),
				(25, !d),
				(30, b),
				(35, false)
			);

			AssertDomain(default, B - A);
		}

		[Test]
		[Combinatorial]
		public static void Difference_with_equal([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(10, c, 20, d);

			//      c____B____d
			//      |         |
			// 5...10...15...20...25
			//      |____A____|
			//      a         b

			var single10 = new Domain<int>(10, true, 10, true);
			var single20 = new Domain<int>(20, true, 20, true);

			switch ((a && !c, b && !d)) {
				case (true, true): AssertDomain(single10 | single20, A - B); break;
				case (true, false): AssertDomain(single10, A - B); break;
				case (false, true): AssertDomain(single20, A - B); break;
				case (false, false): AssertDomain(default, A - B); break;
			}

			AssertContains(A - B,
				(5, false),
				(10, a && !c),
				(15, false),
				(20, b && !d),
				(25, false)
			);

			switch ((c && !a, d && !b)) {
				case (true, true): AssertDomain(single10 | single20, B - A); break;
				case (true, false): AssertDomain(single10, B - A); break;
				case (false, true): AssertDomain(single20, B - A); break;
				case (false, false): AssertDomain(default, B - A); break;
			}

			AssertContains(B - A,
				(5, false),
				(10, c && !a),
				(15, false),
				(20, d && !b),
				(25, false)
			);
		}

		[Test]
		[LowUpCombinatorial]
		public static void Difference_3_and_more(
			[Values(5, 15, 20, 30, 35, 45, 50, 60, 65)] int C_low,
			[Values(10, 15, 25, 30, 40, 45, 55, 60, 70)] int C_up,
			[Values] bool a,
			[Values] bool b,
			[Values] bool c,
			[Values] bool d,
			[Values] bool e,
			[Values] bool f
		) {
			var A = new Domain<int>(15, a, 30, b);
			var B = new Domain<int>(45, c, 60, d);
			var C = new Domain<int>(C_low, e, C_up, f);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70 };

			// TODO: The abominable test. @see Union's abominable test.
			// TODO: Assess that LowUpCombinatorial is as relevant for difference as it is for union.

			AssertDistributivity(Intersection, Difference, A, B, C, testValues);
		}

		[Test]
		public static void Difference_all_in_1() {
			var A = (
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			);

			var B = new Domain<int>(1, true, 3, true);
			var C = new Domain<int>(4, true, 6, true);
			var D = new Domain<int>(10, true, 14, true);
			var E = new Domain<int>(15, true, 16, true);
			var F = new Domain<int>(17, true, 19, true);
			var G = new Domain<int>(20, true, 22, true);
			var H = new Domain<int>(23, true, 24, true);

			//                        __B__    __C__              _______D_______     _E_     ___F___     ___G___     _H_
			//                       |     |  |     |            |               |   |   |   |       |   |       |   |   |
			var testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			//                                   [_____]     [______[]__[]___]                   [___________]
			//                                      A            A    A    A                           A

			AssertDomain(A, A - B);

			AssertDomain((
				  new Domain<int>(6, false, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			), A - C);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(18, true, 21, true)
			), A - D);

			AssertDomain(A, A - E);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(19, false, 21, true)
			), A - F);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 20, false)
			), A - G);

			AssertDomain(A, A - H);

			AssertDomain((
				  new Domain<int>(1, true, 3, true)
				| new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(13, false, 14, true)
				| new Domain<int>(15, true, 16, true)
				| new Domain<int>(18, false, 19, true)
				| new Domain<int>(20, true, 21, false)
			), (B | D | E | F | G) - A);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(19, false, 20, false)
			), A - (B | D | E | F | G));

			AssertDistributivity(Intersection, Difference, A, D, G, testValues);
			AssertDistributivity(Intersection, Difference, A, F, G, testValues);
			AssertDistributivity(Intersection, Difference, A, D, F, testValues);

			AssertDomain((A - B) - C, (A - C) - B, testValues);
			AssertDomain((A - B) - C, A - (B | C), testValues);
			AssertDomain(A, (A - B) | (A - C), testValues);
			AssertDomain((A | B) - (A | C), (B - A) - C, testValues);

			B = new Domain<int>(10, true, 18, true);
			C = new Domain<int>(6, true, 13, true) | new Domain<int>(17, true, 19, true) | new Domain<int>(20, true, 22, true);

			//                          ___________C____________         ___C___     ___C___
			//                         |             _______B___|_______|___    |   |       |
			//                         |            |           |       |   |   |   |       |
			testValues = new[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 17, 18, 19, 20, 21, 22, 23 };
			//                      [_____]     [______[]__[]___]          [____________]
			//                         A            A     A   A                   A

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(18, false, 21, true)
			), A - B);

			AssertDomain((
				  new Domain<int>(5, true, 6, false)
				| new Domain<int>(19, false, 20, false)
			), A - C);

			AssertDomain((
				  new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(13, false, 18, false)
			), B - A);

			AssertDomain((
				  new Domain<int>(13, false, 17, false)
			), B - C);

			AssertDomain((
				  new Domain<int>(7, false, 9, false)
				| new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(17, true, 18, false)
				| new Domain<int>(21, false, 22, true)
			), C - A);

			AssertDomain((
				  new Domain<int>(6, true, 10, false)
				| new Domain<int>(18, false, 19, true)
				| new Domain<int>(20, true, 22, true)
			), C - B);

			AssertDistributivity(Intersection, Difference, A, B, C, testValues);
			AssertDistributivity(Intersection, Difference, B, C, A, testValues);
			AssertDistributivity(Intersection, Difference, C, A, B, testValues);

			AssertDomain((A - B) - C, (A - C) - B, testValues);
			AssertDomain((A - B) - C, A - (B | C), testValues);
			AssertDomain(A, (A - B) | (A - C), testValues);
			AssertDomain((A | B) - (A | C), (B - A) - C, testValues);

			A = (
				  new Domain<int>(1, true, 2, true)
				| new Domain<int>(3, true, 4, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(8, true, 10, true)
				| new Domain<int>(12, true, 15, false)
				| new Domain<int>(17, true, 19, true)
				| new Domain<int>(24, false, 27, false)
				| new Domain<int>(29, true, 31, true)
				| new Domain<int>(32, true, 34, true)
			);

			B = (
				  new Domain<int>(6, true, 8, false)
				| new Domain<int>(14, false, 16, true)
				| new Domain<int>(19, true, 20, true)
				| new Domain<int>(21, true, 22, true)
				| new Domain<int>(23, false, 25, true)
				| new Domain<int>(26, false, 33, false)
			);

			//                                   __B__                        ___B___             _B_     _B_     ___B___     _____________B_____________
			//                                  [     [                      ]       ]           [   ]   [   ]   ]       ]   ]                           [
			testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 };
			//                   [__]  [__]  [__A__]  [__A___]       [_____A_____[       [___A___]                   ]_____A_____[       [___A___]   [___A___]

			AssertDomain((
				  new Domain<int>(1, true, 2, true)
				| new Domain<int>(3, true, 4, true)
				| new Domain<int>(5, true, 6, false)
				| new Domain<int>(8, true, 10, true)
				| new Domain<int>(12, true, 14, true)
				| new Domain<int>(17, true, 19, false)
				| new Domain<int>(25, false, 26, true)
				| new Domain<int>(33, false, 34, true)
			), A - B);

			AssertDomain((
				  new Domain<int>(7, false, 8, false)
				| new Domain<int>(15, false, 16, true)
				| new Domain<int>(19, false, 20, true)
				| new Domain<int>(21, true, 22, true)
				| new Domain<int>(23, false, 24, true)
				| new Domain<int>(27, true, 29, false)
				| new Domain<int>(31, false, 32, false)
			), B - A);

			AssertDomain((A - B) - C, (A - C) - B, testValues);
			AssertDomain((A - B) - C, A - (B | C), testValues);
			AssertDomain(A, (A - B) | (A - C), testValues);
			AssertDomain((A | B) - (A | C), (B - A) - C, testValues);

			A = (
				  new Domain<int>(2, true, 4, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 12, true)
				| new Domain<int>(14, true, 15, true)
				| new Domain<int>(18, true, 23, true)
			);
			
			B = (
				  new Domain<int>(1, true, 3, true)
				| new Domain<int>(6, true, 8, true)
				| new Domain<int>(10, true, 11, true)
				| new Domain<int>(13, true, 16, true)
				| new Domain<int>(17, true, 19, true)
				| new Domain<int>(20, true, 21, true)
				| new Domain<int>(22, true, 24, true)
			);

			//                    __B__          __B__        _B_         _____B_____     ___B___     _B_     ___B___
			//                   |     |        |     |      |   |       |           |   |       |   |   |   |       |
			testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			//                      |__A__|  |__A__|     |_____A_____|       |_A_|           |_________A_________|

			AssertDomain((
				  new Domain<int>(3, false, 4, true)
				| new Domain<int>(5, true, 6, false)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(11, false, 12, true)
				| new Domain<int>(19, false, 20, false)
				| new Domain<int>(21, false, 22, false)
			), A - B);

			AssertDomain((
				  new Domain<int>(1, true, 2, false)
				| new Domain<int>(7, false, 8, true)
				| new Domain<int>(13, true, 14, false)
				| new Domain<int>(15, false, 16, true)
				| new Domain<int>(17, true, 18, false)
				| new Domain<int>(23, false, 24, true)
			), B - A);
		}

		// DisjunctiveUnion
		// ----

		readonly static Operation<int> DisjunctiveUnion = (a, b) => a ^ b;

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_default() {
			var A = new Domain<int>(10, true, 20, true);
			var B = default(Domain<int>);

			AssertDomain(A, A ^ B);
			AssertCommutativity(DisjunctiveUnion, A, B);
		}

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_other([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(30, c, 40, d);

			//  5...10...15...20...25...30...35...40...45
			//       |____A____|         |____B____|
			//       a         b         c         d

			AssertDomain(A | B, A ^ B);
			AssertCommutativity(DisjunctiveUnion, A, B);
		}

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_touching([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(20, c, 30, d);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |___A___|___B___|
			//                           a      bc       d

			var Df = A ^ B;

			if (b ^ c) AssertDomain(new Domain<int>(10, true, 30, true), Df);
			else AssertDomain(new Domain<int>(10, true, 20, false) | new Domain<int>(20, false, 30, true), Df);

			AssertContains(Df,
				(5, false),
				(10, a),
				(15, true),
				(20, b ^ c),
				(25, true),
				(30, d),
				(35, false)
			);

			AssertIdentity(DisjunctiveUnion, A, testValues);
			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_overlap([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 25, b);
			var B = new Domain<int>(20, c, 35, d);

			//                                   c_____B_____d
			//                                   |           |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40 };
			//                           |_____A____|
			//                           a          b

			AssertDomain(new Domain<int>(10, a, 20, !c) | new Domain<int>(25, !b, 35, d), A ^ B);
			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_sub([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 30, b);
			var B = new Domain<int>(15, c, 25, d);

			//                               c___B___d
			//                               |       |
			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35 };
			//                           |_______A_______|
			//                           a               b

			AssertDomain(new Domain<int>(10, a, 15, !c) | new Domain<int>(25, !d, 30, b), A ^ B);
			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
		}

		[Test]
		[Combinatorial]
		public static void DisjunctiveUnion_with_equal([Values] bool a, [Values] bool b, [Values] bool c, [Values] bool d) {
			var A = new Domain<int>(10, a, 20, b);
			var B = new Domain<int>(10, c, 20, d);

			//                           c___B___d
			//                           |       |
			var testValues = new[] { 5, 10, 25, 20, 25 };
			//                           |___A___|
			//                           a       b

			AssertContains(A ^ B,
				(5, false),
				(10, a ^ c),
				(15, false),
				(20, b ^ d),
				(25, false)
			);

			var single10 = new Domain<int>(10, true, 10, true);
			var single20 = new Domain<int>(20, true, 20, true);

			switch ((a ^ c, b ^ d)) {
				case (true, true): AssertDomain(single10 | single20, A ^ B); break;
				case (true, false): AssertDomain(single10, A ^ B); break;
				case (false, true): AssertDomain(single20, A ^ B); break;
				case (false, false): AssertDomain(default, A ^ B); break;
			}

			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
		}

		[Test]
		[LowUpCombinatorial]
		public static void DisjunctiveUnion_3_and_more(
			[Values(5, 15, 20, 30, 35, 45, 50, 60, 65)] int C_low,
			[Values(10, 15, 25, 30, 40, 45, 55, 60, 70)] int C_up,
			[Values] bool a,
			[Values] bool b,
			[Values] bool c,
			[Values] bool d,
			[Values] bool e,
			[Values] bool f
		) {
			var A = new Domain<int>(15, a, 30, b);
			var B = new Domain<int>(45, c, 60, d);
			var C = new Domain<int>(C_low, e, C_up, f);

			var testValues = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70 };

			// TODO: The abominable test. @see Union's abominable test.
			// TODO: Assess that LowUpCombinatorial is as relevant for Disjunctive union as it is for union.

			AssertAssociativity(DisjunctiveUnion, A, B, C, testValues);
		}

		[Test]
		public static void DisjunctiveUnion_with_non_touching_parts() {
			var A = new Domain<int>(1, true, 2, true);
			var B = new Domain<int>(2, true, 3, true);
			var C = new Domain<int>(3, true, 4, true);

			AssertDomain(new Domain<int>(1, true, 1, true) | new Domain<int>(3, true, 3, true), A ^ B);
			AssertDomain(new Domain<int>(1, true, 1, true) | new Domain<int>(4, true, 4, true), A ^ B ^ C);
			AssertCommutativity(DisjunctiveUnion, A, B);
			AssertAssociativity(DisjunctiveUnion, A, B, C);

			A = new Domain<int>(1, true, 2, true);
			B = new Domain<int>(3, true, 4, true);
			C = new Domain<int>(5, true, 6, true);

			// [1..2] and [3..4] are not touching, i.e. there is a "gap" between 2 and 3,
			// event if int doesn't define any value "between" integers,
			// IComparable doesn't know about it.
			AssertDomain(A | B | C, A ^ B ^ C);
			AssertAssociativity(DisjunctiveUnion, A, B, C);
		}

		[Test]
		public static void DisjunctiveUnion_all_in_1() {
			var A = (
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 21, true)
			);

			var B = new Domain<int>(1, true, 3, true);
			var C = new Domain<int>(4, true, 6, true);
			var D = new Domain<int>(10, true, 14, true);
			var E = new Domain<int>(15, true, 16, true);
			var F = new Domain<int>(17, true, 19, true);
			var G = new Domain<int>(20, true, 22, true);
			var H = new Domain<int>(23, true, 24, true);

			//                        __B__    __C__              _______D_______     _E_     ___F___     ___G___     _H_
			//                       |     |  |     |            |               |   |   |   |       |   |       |   |   |
			var testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
			//                                   [_____]     [______[]__[]___]                   [___________]
			//                                      A            A    A    A                           A

			AssertDomain(A | B, A ^ B);
			AssertDomain(new Domain<int>(5, true, 6, true), A ^ C);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(13, false, 14, true)
				| new Domain<int>(18, true, 21, true)
			), A ^ D);

			AssertDomain(A | E, A ^ E);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(17, true, 18, false)
				| new Domain<int>(19, false, 21, true)
			), A ^ F);

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 20, false)
				| new Domain<int>(21, false, 22, true)
			), A ^ G);

			AssertDomain(A | H, A ^ H);

			AssertDomain((
				  new Domain<int>(1, true, 3, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 13, true)
				| new Domain<int>(18, true, 20, false)
				| new Domain<int>(21, false, 22, true)
			), A ^ (B | G));

			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
			AssertCommutativity(DisjunctiveUnion, A, C, testValues);
			AssertCommutativity(DisjunctiveUnion, A, D, testValues);
			AssertCommutativity(DisjunctiveUnion, A, E, testValues);
			AssertCommutativity(DisjunctiveUnion, A, F, testValues);
			AssertCommutativity(DisjunctiveUnion, A, G, testValues);
			AssertCommutativity(DisjunctiveUnion, A, H, testValues);

			AssertAssociativity(DisjunctiveUnion, A, B, C, testValues);
			AssertAssociativity(DisjunctiveUnion, A, C, F, testValues);
			AssertAssociativity(DisjunctiveUnion, A, C, G, testValues);
			AssertAssociativity(DisjunctiveUnion, A, D, E, testValues);
			AssertAssociativity(DisjunctiveUnion, A, D, H, testValues);

			B = new Domain<int>(10, true, 18, true);
			C = new Domain<int>(6, true, 13, true) | new Domain<int>(17, true, 19, true) | new Domain<int>(20, true, 22, true);

			//                          ___________C____________                 ___C___     ___C___
			//                         |             _______B___|_______________|___    |   |       |
			//                         |            |           |               |   |   |   |       |
			testValues = new[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
			//                      [_____]     [______[]__[]___]                  [____________]
			//                         A            A     A   A                           A

			AssertDomain((
				  new Domain<int>(5, true, 7, true)
				| new Domain<int>(9, true, 10, false)
				| new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(13, false, 18, false)
				| new Domain<int>(18, false, 21, true)
			), A ^ B);

			AssertDomain((
				  new Domain<int>(5, true, 6, false)
				| new Domain<int>(7, false, 9, false)
				| new Domain<int>(11, true, 11, true)
				| new Domain<int>(12, true, 12, true)
				| new Domain<int>(17, true, 18, false)
				| new Domain<int>(19, false, 20, false)
				| new Domain<int>(21, false, 22, true)
			), A ^ C);

			AssertDomain((
				  new Domain<int>(5, true, 6, false)
				| new Domain<int>(7, false, 9, false)
				| new Domain<int>(10, true, 11, false)
				| new Domain<int>(11, false, 12, false)
				| new Domain<int>(12, false, 17, false)
				| new Domain<int>(18, true, 18, true)
				| new Domain<int>(19, false, 20, false)
				| new Domain<int>(21, false, 22, true)
			), A ^ B ^ C);

			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
			AssertCommutativity(DisjunctiveUnion, A, C, testValues);
			AssertAssociativity(DisjunctiveUnion, A, B, C, testValues);

			A = (
				  new Domain<int>(1, true, 2, true)
				| new Domain<int>(3, true, 4, true)
				| new Domain<int>(5, true, 7, true)
				| new Domain<int>(8, true, 10, true)
				| new Domain<int>(12, true, 15, false)
				| new Domain<int>(17, true, 19, true)
				| new Domain<int>(24, false, 27, false)
				| new Domain<int>(29, true, 31, true)
				| new Domain<int>(32, true, 34, true)
			);

			B = (
				  new Domain<int>(6, true, 8, false)
				| new Domain<int>(14, false, 16, true)
				| new Domain<int>(19, true, 20, true)
				| new Domain<int>(21, true, 22, true)
				| new Domain<int>(23, false, 25, true)
				| new Domain<int>(26, false, 33, false)
			);

			//                                   __B__                        ___B___             _B_     _B_     ___B___     _____________B_____________
			//                                  [     [                      ]       ]           [   ]   [   ]   ]       ]   ]                           [
			testValues = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 };
			//                   [__]  [__]  [__A__]  [__A___]       [_____A_____[       [___A___]                   ]_____A_____[       [___A___]   [___A___]

			AssertDomain((
				  new Domain<int>(1, true, 2, true)
				| new Domain<int>(3, true, 4, true)
				| new Domain<int>(5, true, 6, false)
				| new Domain<int>(7, false, 10, true)
				| new Domain<int>(12, true, 14, true)
				| new Domain<int>(15, true, 16, true)
				| new Domain<int>(17, true, 19, false)
				| new Domain<int>(19, false, 20, true)
				| new Domain<int>(21, true, 22, true)
				| new Domain<int>(23, false, 24, true)
				| new Domain<int>(25, false, 26, true)
				| new Domain<int>(27, true, 29, false)
				| new Domain<int>(31, false, 32, false)
				| new Domain<int>(33, true, 34, true)
			), A ^ B);

			AssertCommutativity(DisjunctiveUnion, A, B, testValues);
		}
	}

	public readonly struct Domain<T> : IEquatable<Domain<T>> where T : IComparable<T> {
		readonly struct Part {
			public readonly T Lower;
			public readonly bool LowerIncluded;
			public readonly T Upper;
			public readonly bool UpperIncluded;

			public Part(T lower, bool lowerIncluded, T upper, bool upperIncluded) {
				Lower = lower;
				LowerIncluded = lowerIncluded;
				Upper = upper;
				UpperIncluded = upperIncluded;
			}

			public void Deconstruct(out T lower, out bool lowerIncluded, out T upper, out bool upperIncluded) {
				lower = Lower;
				lowerIncluded = LowerIncluded;
				upper = Upper;
				upperIncluded = UpperIncluded;
			}

			//public void ToStringBuilder(StringBuilder @out) {
			//	@out.Append(LowerIncluded ? '[' : ']');
			//	@out.Append(Lower);
			//	@out.Append(';');
			//	@out.Append(Upper);
			//	@out.Append(UpperIncluded ? ']' : '[');
			//}
		}

		readonly ImmutableArray<Part> Parts;
		Domain(ImmutableArray<Part> parts) => Parts = parts;

		public Domain(T value) => Parts = ImmutableArray.Create(new Part(value, true, value, true));

		public Domain(T lower, /*               */ T upper /*              */ ) : this(upper, /*    */ true, lower, /*    */ true) {}
		public Domain(T lower, bool lowerIncluded, T upper /*              */ ) : this(upper, lowerIncluded, lower, /*    */ true) {}
		public Domain(T lower, /*               */ T upper, bool upperIncluded) : this(upper, /*    */ true, lower, upperIncluded) {}
		public Domain(T lower, bool lowerIncluded, T upper, bool upperIncluded) {
			var comparison = lower.CompareTo(upper);

			if (comparison == 0 && !(lowerIncluded && upperIncluded))
				throw new ArgumentException("Equal bounds were exclusive.", nameof(upper));

			if (comparison > 0)
				throw new ArgumentException("Bounds were unordered.", nameof(upper));

			Parts = ImmutableArray.Create(new Part(lower, lowerIncluded, upper, upperIncluded));
		}

		//public Domain(params (T, T)[] bounds) {
		//	var builder = ImmutableArray.CreateBuilder<(T, bool)>(bounds.Length);

		//	for (int i = 0; i < bounds.Length; ++i)
		//		builder[i] = (bounds[i], true);

		//	Bounds = builder.ToImmutable();
		//}

		//public Domain(params (T Value, bool Included)[] bounds) {
		//	if (bounds.Length == 0 || bounds.Length % 2 == 1)
		//		throw new ArgumentException("Domain bounds must come in pairs.", nameof(bounds));

		//	Bounds = ImmutableArray.Create(bounds);
		//}

		public bool DoesNotContain(T value) => !Contains(value);
		public bool Contains(T value) {
			if (Parts == null || Parts.Length == 0)
				return false;

			for (int i = 0; i < Parts.Length; ++i) {
				var (lower, lowerIncluded, upper, upperIncluded) = Parts[i];

				if (lower.ComesBefore(value, lowerIncluded) && value.ComesBefore(upper, upperIncluded))
					return true;
			}

			return false;
		}

		// https://www.learnbyexample.org/wp-content/uploads/python/Python-Set-Operatioons.png

		/// <summary> Union. </summary>
		public static Domain<T> operator |(Domain<T> a, Domain<T> b) {
			if (a.Parts == null || a.Parts.Length == 0) return b;
			if (b.Parts == null || b.Parts.Length == 0) return a;

			var parts = ImmutableArray.CreateBuilder<Part>(a.Parts.Length + b.Parts.Length);

			parts.AddRange(a.Parts);

			// For each new part, test the new part for overlap with existing parts.
			var i = 0;
			for (var j = 0; j < b.Parts.Length; ++j) {
				var bPart = b.Parts[j];

				// Move forward while the existing part comes before the new part: we're not in position yet.
				while (i < parts.Count && parts[i].Upper.ComesBefore(bPart.Lower, !(parts[i].UpperIncluded || bPart.LowerIncluded)))
					++i;

				// Merge and remove while the existing part overlaps the new part.
				while (i < parts.Count && parts[i].Lower.ComesBefore(bPart.Upper, parts[i].LowerIncluded || bPart.UpperIncluded)) {
					bPart = Merge(bPart, parts[i]);
					parts.RemoveAt(i);
				}

				parts.Insert(i, bPart);
			}

			return new Domain<T>(parts.ToImmutable());
		}

		static Part Merge(Part a, Part b) {
			var (lower, lowerIncluded) = a.Lower.CompareTo(b.Lower) switch {
				< 0 => (a.Lower, a.LowerIncluded),
				  0 => (a.Lower, a.LowerIncluded || b.LowerIncluded),
				> 0 => (b.Lower, b.LowerIncluded)
			};

			var (upper, upperIncluded) = a.Upper.CompareTo(b.Upper) switch {
				< 0 => (b.Upper, b.UpperIncluded),
				  0 => (a.Upper, a.UpperIncluded || b.UpperIncluded),
				> 0 => (a.Upper, a.UpperIncluded)
			};

			return new Part(lower, lowerIncluded, upper, upperIncluded);
		}

		/// <summary> Intersection. </summary>
		public static Domain<T> operator &(Domain<T> a, Domain<T> b) {
			if (a.Parts == null || a.Parts.Length == 0) return default;
			if (b.Parts == null || b.Parts.Length == 0) return default;

			var parts = ImmutableArray.CreateBuilder<Part>(Math.Max(a.Parts.Length, b.Parts.Length));

			// For each new part, test the new part for overlap with existing parts.
			var i = 0;
			foreach (var bPart in b.Parts) {
				// Move forward while the existing part comes before the new part: we're not in position yet.
				while (i < a.Parts.Length && a.Parts[i].Upper.ComesBefore(bPart.Lower, !(a.Parts[i].UpperIncluded && bPart.LowerIncluded)))
					++i;

				// Intersect and insert while the existing part overlaps the new part.
				while (i < a.Parts.Length && a.Parts[i].Lower.ComesBefore(bPart.Upper, a.Parts[i].LowerIncluded && bPart.UpperIncluded)) {
					parts.Add(Intersect(bPart, a.Parts[i]));
					++i;
				}

				// A hack to avoid skipping A-parts overlapping with 2 B-parts.
				if (i != 0)
					--i;
			}

			return new Domain<T>(parts.ToImmutable());
		}

		static Part Intersect(Part a, Part b) {
			var (lower, lowerIncluded) = a.Lower.CompareTo(b.Lower) switch {
				< 0 => (b.Lower, b.LowerIncluded),
				  0 => (b.Lower, a.LowerIncluded && b.LowerIncluded),
				> 0 => (a.Lower, a.LowerIncluded)
			};

			var (upper, upperIncluded) = a.Upper.CompareTo(b.Upper) switch {
				< 0 => (a.Upper, a.UpperIncluded),
				  0 => (b.Upper, a.UpperIncluded && b.UpperIncluded),
				> 0 => (b.Upper, b.UpperIncluded)
			};

			return new Part(lower, lowerIncluded, upper, upperIncluded);
		}

		/// <summary> Difference. </summary>
		public static Domain<T> operator -(Domain<T> a, Domain<T> b) {
			if (a.Parts == null || a.Parts.Length == 0) return default;
			if (b.Parts == null || b.Parts.Length == 0) return a;

			var parts = ImmutableArray.CreateBuilder<Part>(a.Parts.Length + b.Parts.Length);

			//parts.AddRange(a.Parts);

			//// For each new part, test the new part for overlap with existing parts.
			//var i = 0;
			//for (var j = 0; j < b.Parts.Length; ++j) {
			//	var bPart = b.Parts[j];

			//	// Move forward while the existing part comes before the new part: we're not in position yet.
			//	while (i < parts.Count && parts[i].Upper.ComesBefore(bPart.Lower, !(parts[i].UpperIncluded || bPart.LowerIncluded)))
			//		++i;

			//	// Merge and remove while the existing part overlaps the new part.
			//	while (i < parts.Count && parts[i].Lower.ComesBefore(bPart.Upper, parts[i].LowerIncluded || bPart.UpperIncluded)) {
			//		var aPart = parts[i];

			//		// If there's some of A left on the lower side, subtract and insert it.
			//		if (aPart.Lower.ComesBefore(bPart.Lower, aPart.LowerIncluded && !bPart.LowerIncluded))
			//			parts[i] = new Part(aPart.Lower, aPart.LowerIncluded, bPart.Lower, !bPart.LowerIncluded);

			//		// If there's some of A left on the upper side, drop the lower side.
			//		if (bPart.Upper.ComesBefore(aPart.Upper, aPart.UpperIncluded && !bPart.UpperIncluded))
			//			parts.Insert(++i, new Part(bPart.Upper, !bPart.UpperIncluded, aPart.Upper, aPart.UpperIncluded));
			//	}
			//}

			/*

			foreach bPart {
				while !overlap
					skip

				while overlap
					if left side remains
						replace current by left
					if right side remains
						swap
			}

			*/

			//// For each new part, test the new part for overlap with existing parts.
			//var j = 0;
			//for (var i = 0; i < a.Parts.Length; ++i) {
			//	var aPart = a.Parts[i];

			//	// Move forward while the subtracted part comes before the base part: we're not in position yet.
			//	while (j < b.Parts.Length && b.Parts[j].Upper.ComesBefore(aPart.Lower, !(b.Parts[j].UpperIncluded && aPart.LowerIncluded)))
			//		++j;

			//	// Insert A if this was the last B-part, or if the next B-part doesn't overlap.
			//	if (b.Parts.Length <= j || aPart.Upper.ComesBefore(b.Parts[j].Lower, !(aPart.UpperIncluded && b.Parts[j].LowerIncluded)))
			//		parts.Add(aPart);

			//	// Subtract and insert the overlaping parts.
			//	else do {
			//		// If there's some of A left on the lower side, subtract and insert it.
			//		if (aPart.Lower.ComesBefore(b.Parts[j].Lower, aPart.LowerIncluded && ! b.Parts[j].LowerIncluded))
			//			parts.Add(new Part(aPart.Lower, aPart.LowerIncluded, b.Parts[j].Lower, !b.Parts[j].LowerIncluded));

			//		// If there's some of A left on the upper side, drop the lower side.
			//		if (b.Parts[j].Upper.ComesBefore(aPart.Upper, aPart.UpperIncluded && !b.Parts[j].UpperIncluded)) {
			//			aPart = new Part(b.Parts[j].Upper, !b.Parts[j].UpperIncluded, aPart.Upper, aPart.UpperIncluded);
			//			++j;

			//			// Insert the remaining of A if this was the last B-part, or if the next B-part doesn't overlap.
			//			if (b.Parts.Length <= j || aPart.Upper.ComesBefore(b.Parts[j].Lower, !(aPart.UpperIncluded && b.Parts[j].LowerIncluded))) {
			//				parts.Add(aPart);
			//				break;
			//			}
			//		}

			//		else ++j;
			//	} while (j < b.Parts.Length && b.Parts[j].Lower.ComesBefore(aPart.Upper, b.Parts[j].LowerIncluded && aPart.UpperIncluded));
			//}

			return new Domain<T>(parts.ToImmutable());
		}

		/// <summary> Symmetric difference, a.k.a. Disjunctive union. </summary>
		public static Domain<T> operator ^(Domain<T> a, Domain<T> b) => default;

		// TODO Difference graal

		// TODO use this for DisjunctiveUnion: public static Domain<T> operator ^(Domain<T> a, Domain<T> b) => (a | b) - (a & b);
		// TODO benchmark the DisjunctiveUnion
		// TODO DisjunctiveUnion graal
		// TODO benchmark the DisjunctiveUnion again

		//public static Domain<T> operator +(T a, Domain<T> b) => b + a;
		//public static Domain<T> operator +(Domain<T> a, T b) => default;

		//public static Domain<T> operator -(T a, Domain<T> b) => b + a;
		//public static Domain<T> operator -(Domain<T> a, T b) => default;

		public static bool operator ==(Domain<T> a, Domain<T> b) => a.Equals(b);
		public static bool operator !=(Domain<T> a, Domain<T> b) => !a.Equals(b);

		public bool Equals(Domain<T> other) =>
			  (Parts == null || Parts.Length == 0) ? other.Parts == null || other.Parts.Length == 0
			: (other.Parts == null || other.Parts.Length == 0) ? false
			: Parts.SequenceEqual(other.Parts);

		public override bool Equals(object? obj) => obj is Domain<T> other && Equals(other);
		public override int GetHashCode() =>
			Parts == null || Parts.Length == 0
				? 0
				: Parts.Aggregate(0, (hash, part) => hash ^ part.GetHashCode());

		//public override string ToString() {
		//	if (Parts == null || Parts.Length == 0)
		//		return string.Empty;

		//	var @out = new StringBuilder(Parts.Length * 15);

		//	Parts[0].ToStringBuilder(@out);

		//	for (var i = 1; i < Parts.Length; ++i) {
		//		@out.Append('U');
		//		Parts[i].ToStringBuilder(@out);
		//	}

		//	return @out.ToString();
		//}
	}

	public static class Domain {
		//public static readonly Domain<char> Chars = new(char.MinValue, char.MaxValue);
		//public static readonly Domain<sbyte> SBytes = new(sbyte.MinValue, sbyte.MaxValue);
		//public static readonly Domain<byte> Bytes = new(byte.MinValue, byte.MaxValue);
		//public static readonly Domain<short> Shorts = new(short.MinValue, short.MaxValue);
		//public static readonly Domain<ushort> UShorts = new(ushort.MinValue, ushort.MaxValue);
		//public static readonly Domain<int> Ints = new(int.MinValue, int.MaxValue);
		//public static readonly Domain<uint> UInts = new(uint.MinValue, uint.MaxValue);
		//public static readonly Domain<long> Longs = new(long.MinValue, long.MaxValue);
		//public static readonly Domain<ulong> ULongs = new(ulong.MinValue, ulong.MaxValue);
		//public static readonly Domain<Half> Halves = new(Half.NegativeInfinity, Half.PositiveInfinity);
		//public static readonly Domain<float> Floats = new(float.NegativeInfinity, float.PositiveInfinity);
		//public static readonly Domain<double> Doubles = new(double.NegativeInfinity, double.PositiveInfinity);
		//public static readonly Domain<decimal> Decimals = new(decimal.MinValue, decimal.MaxValue);
		//public static readonly Domain<DateTime> DateTimes = new(DateTime.MinValue, DateTime.MaxValue);
		//public static readonly Domain<DateTimeOffset> DateTimeOffsets = new(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
		//public static readonly Domain<TimeSpan> TimeSpans = new(TimeSpan.MinValue, TimeSpan.MaxValue);
		//public static readonly Domain<Guid> Guids = new(Guid.Empty, new Guid(uint.MaxValue, ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		//public static readonly Domain<Version> Versions = new(new Version(), new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));

		internal static bool ComesBefore<T>(this T @this, T other, bool inclusive) where T : IComparable<T> =>
			inclusive
				? @this.CompareTo(other) <= 0
				: @this.CompareTo(other) < 0;
	}
}
