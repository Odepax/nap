using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static Nap.Framework.Testing.HttpResponseTests;

namespace Nap.PrototypingApi.Tests.Cat
{
	public static class Retrieve
	{
		[Test]
		public static void R()
		{
			Assert.Pass("Same as Create.C");
		}

		[Test]
		public static async Task R_404()
		{
			Guid nonExisting = new Guid(404, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			await Api.Endpoints.Cat.Select(nonExisting.ToString())
				.Get()
				.AssertResponse(
					StatusCode(NotFound)
				);
		}

		[Test]
		[NonParallelizable]
		public static async Task Ra_0()
		{
			await Api.Endpoints.Cat
				.Get()
				.AssertResponse(
					JsonContent(new object[0])
				);
		}

		[Test]
		[NonParallelizable]
		public static async Task Ra_1()
		{
			Guid created = Guid.Empty;

			await Api.Endpoints.Cat
				.Post(new
				{
					name = "C",
					purrPower = 1,
					isGrumpy = true
				})
				.AssertResponse(
					CreatedWithId(Api.Endpoints.Cat.Url + "/{0}", id => created = id)
				);

			await Api.Endpoints.Cat
				.Get()
				.AssertResponse(
					JsonContent(new[] {
						new
						{
							id = created.ToString(),
							name = "C",
							purrPower = 1,
							isGrumpy = true
						}
					})
				);

			await Api.Endpoints.Cat.Select(created.ToString())
				.Delete()
				.AssertResponse(
					StatusCode(NoContent)
				);
		}

		[Test]
		[NonParallelizable]
		public static async Task Ra_N()
		{
			Guid[] created = new Guid[3];

			await Task.WhenAll(Enumerable.Range(0, 3).Select(i =>
				Api.Endpoints.Cat
					.Post(new { name = $"C{ i }", purrPower = 1, isGrumpy = true })
					.AssertResponse(
						CreatedWithId(Api.Endpoints.Cat.Url + "/{0}", id => created[i] = id)
					)
			));

			await Api.Endpoints.Cat
				.Get()
				.AssertResponse(
					JsonContent(new[]
					{
						new { id = created[0].ToString(), name = "C0", purrPower = 1, isGrumpy = true },
						new { id = created[1].ToString(), name = "C1", purrPower = 1, isGrumpy = true },
						new { id = created[2].ToString(), name = "C2", purrPower = 1, isGrumpy = true }
					})
				);

			await Task.WhenAll(created.Select(id =>
				Api.Endpoints.Cat.Select(id.ToString())
					.Delete()
					.AssertResponse(
						StatusCode(NoContent)
					)
			));
		}
	
		[Test]
		public static void jso()
		{
			JsonElement a = JsonDocument.Parse(
				JsonSerializer.Serialize(new object[] {
					new
					{
						id = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
						name = "C0",
						purrPower = 1,
						isGrumpy = true,
					},
					new
					{
						id = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
						name = "C2",
						purrPower = 1,
						isGrumpy = true,
					}
				},
				new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
					IgnoreNullValues = false,
					IgnoreReadOnlyProperties = false,
					WriteIndented = true
				})
			).RootElement;

			JsonElement b = JsonDocument.Parse(
				JsonSerializer.Serialize(new object[] {
					new
					{
						name = "C2",
						purrPower = 1,
						isGrumpy = true,
						id = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					},
					new
					{
						name = "C0",
						purrPower = 1,
						isGrumpy = true,
						id = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					}
				},
				new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
						DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
						IgnoreNullValues = false,
						IgnoreReadOnlyProperties = false,
						WriteIndented = true
					})
			).RootElement;

			Assert.IsTrue(TestJson(a, b));
		}
	}
}
