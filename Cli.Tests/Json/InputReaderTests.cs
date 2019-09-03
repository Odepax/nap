using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nap.Cli.Definitions;
using NUnit.Framework;

namespace Nap.Cli.Json
{
	[TestFixture]
	public class InputReaderTests
	{
		[Test]
		[TestCase(@"")]
		[TestCase(@"[]")]
		[TestCase(@"{
			""cat"": [
				{ ""name"": ""purr power"", ""type"": ""float"" }
			]
		}")]
		public void It_does_not_read_ill_formed(string @in)
		{
			using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(@in));

			Assert.Catch(() => InputReader.ReadFromJson(stream));
		}

		[Test]
		[TestCaseSource(nameof(JsonSamples))]
		public void It_reads_well_formed(string @in, IEnumerable<Resource> expected)
		{
			using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(@in));

			AssertResourceList(expected, InputReader.ReadFromJson(stream));
		}

		public static readonly object[] JsonSamples =
		{
			new object[]
			{
				@"{
				}",
				new Resource[0]
			},
			new object[]
			{
				@"{
					""cat"": {}
				}",
				new[]
				{
					new Resource("cat", new Dictionary<string, string>(0))
				}
			},
			new object[]
			{
				@"{
					""cat"": {},
					""bird"": {}
				}",
				new[]
				{
					new Resource("cat", new Dictionary<string, string>(0)),
					new Resource("bird", new Dictionary<string, string>(0))
				}
			},
			new object[]
			{
				@"{
					""cat"": {
						""name"": ""string"",
						""purr power"": ""float""
					}
				}",
				new[]
				{
					new Resource("cat", new Dictionary<string, string>
					{
						["name"] = "string",
						["purr power"] = "float"
					})
				}
			},
			new object[]
			{
				@"{
					""cat"": {
						""name"": ""string"",
						""purr power"": ""float""
					},
					""bird"": {
						""species"": ""string"",
						""can fly"": ""bool""
					}
				}",
				new[]
				{
					new Resource("cat", new Dictionary<string, string>
					{
						["name"] = "string",
						["purr power"] = "float"
					}),
					new Resource("bird", new Dictionary<string, string>
					{
						["species"] = "string",
						["can fly"] = "bool"
					})
				}
			}
		};

		private static void AssertResourceList(IEnumerable<Resource> expected, IEnumerable<Resource> actual)
		{
			var e = expected.ToArray();
			var a = actual.ToArray();

			Assert.AreEqual(e.Length, a.Length);

			for (int i = 0; i < e.Length; ++i)
			{
				Assert.AreEqual(e[i].Name, a[i].Name);
				Assert.AreEqual(e[i].Fields, a[i].Fields);
			}
		}
	}
}
