using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Nap.Tests {
	static class TryGetMetaTests {
		static readonly IntType intType = new() {
			Meta = new Dictionary<string, object?> {
				[NapBuiltInMeta.Min] = 12,
				[NapBuiltInMeta.IsOptional] = true
			}
		};

		static readonly ResourceType resourceType = new() {
			Meta = new Dictionary<string, object?> {
				[NapBuiltInMeta.SelfReference] = SelfReference.Forbid,
				["yolo"] = new[] {
					new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
				}
			}
		};

		[Test]
		public static void HappyPath() {
			Assert.IsTrue(intType.TryGetMin(out var min));
			Assert.AreEqual(12, min);

			Assert.IsFalse(intType.TryGetMax(out var max));
			Assert.IsNull(max);

			Assert.IsFalse(intType.TryGetMinIsInclusive(out var minIsInclusive));
			Assert.IsTrue(minIsInclusive);

			Assert.IsFalse(resourceType.TryGetIsOptional(out var isOptional));
			Assert.IsFalse(isOptional);

			Assert.IsTrue(resourceType.TryGetSelfReference(out var selfReference));
			Assert.AreEqual(SelfReference.Forbid, selfReference);

			Assert.IsTrue(resourceType.TryGetMeta<IReadOnlyCollection<Guid>>("yolo", out var yolo));
			Assert.AreEqual(new[] {
				new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
				new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
				new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
			}, yolo);
		}

		[Test]
		public static void SadPath() {
			Assert.IsFalse(intType.TryGetMeta<float>(NapBuiltInMeta.Min, out var min, 42f));
			Assert.AreEqual(42f, min);
			
			Assert.IsFalse(resourceType.TryGetMeta<Guid>("yolo", out var yolo));
			Assert.AreEqual(Guid.Empty, yolo);
		}
	}
}
