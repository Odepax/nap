using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Nap.Tests {
	static class NapAssert {
		public static void AssertContext(PartialContext expected, PartialContext actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Context names didn't match.");
			Assert.AreEqual(expected.Containers.Count, actual.Containers.Count, "Containers are not the same count.");

			using var expectedEnumerator = expected.Containers.GetEnumerator();
			using var actualEnumerator = actual.Containers.GetEnumerator();

			while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
				AssertContainer(expectedEnumerator.Current, actualEnumerator.Current);
		}

		public static void AssertContainer(PartialContainer expected, PartialContainer actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Container names didn't match.");
			Assert.AreEqual(expected.Resources.Count, actual.Resources.Count, "Resources are not the same count.");

			using var expectedEnumerator = expected.Resources.GetEnumerator();
			using var actualEnumerator = actual.Resources.GetEnumerator();

			while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
				AssertResource(expectedEnumerator.Current, actualEnumerator.Current);
		}

		public static void AssertResource(PartialResource expected, PartialResource actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Resource names didn't match.");
			Assert.AreEqual(expected.GenericTemplates.Count, actual.GenericTemplates.Count, "Resource's generic templates are not the same count.");
			Assert.AreEqual(expected.Fields.Count, actual.Fields.Count, "Resource's fields are not the same count.");

			foreach (var (i, expectedTemplate) in expected.GenericTemplates)
				Assert.AreEqual(expectedTemplate, actual.GenericTemplates[i], $"Resource generic templates [{ i }] didn't match");

			foreach (var (fieldName, expectedFieldType) in expected.Fields)
				AssertType(expectedFieldType, actual.Fields[fieldName]);
		}

		static void AssertType(PartialFieldType expected, PartialFieldType actual) {
			Assert.AreEqual(expected.Name, actual.Name, "Field type names didn't match.");
			Assert.AreEqual(expected.Generics.Count, actual.Generics.Count, "Field type's generic are not the same count.");
			Assert.AreEqual(expected.Meta.Count, actual.Meta.Count, "Field type's meta are not the same count.");

			foreach (var (i, expectedGeneric) in expected.Generics)
				AssertType(expectedGeneric, actual.Generics[i]);

			foreach (var (metaName, expectedMeta) in expected.Meta)
				if (expectedMeta is Regex expectedRegex) {
					if (actual.Meta[metaName] is Regex actualRegex) {
						Assert.AreEqual(expectedRegex.ToString(), actualRegex.ToString(), $"Field type meta '{ metaName }' didn't match");
						Assert.AreEqual(expectedRegex.Options, actualRegex.Options, $"Field type meta '{ metaName }' didn't match");
					}

					else Assert.Fail($"Field type meta '{ metaName }' didn't match");
				}

				else Assert.AreEqual(expectedMeta, actual.Meta[metaName], $"Field type meta '{ metaName }' didn't match");
		}
	}
}
