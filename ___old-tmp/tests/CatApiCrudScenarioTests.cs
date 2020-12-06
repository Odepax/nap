using System;
using System.Net.Http;
using Nap.CsGeneration.Tests.Tools;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static System.Net.Mime.MediaTypeNames;

namespace Nap.CsGeneration.Tests
{
	[TestFixture]
	[SingleThreaded]
	public class CatApiCrudScenarioTests
	{
		private const string BaseUri = "http://localhost:52806";

		private static readonly HttpMessageInvoker Http = new HttpClient
		{
			BaseAddress = new Uri(BaseUri)
		};

		private static Guid CreatedCatId = Guid.Empty;

		[Test]
		public void Step_00_00()
		{
			Http
				.Get("/api/cats")
				.AssertAgainst(
					HttpResponseTests.StatusCode(OK),
					HttpResponseTests.ContentType(Application.Json),
					HttpResponseTests.JsonContent(new object[0])
				);
		}

		[Test]
		public void Step_01_00()
		{
			Http
				.Post("/api/cats", new
				{
					name = "Felix",
					purrPower = 8
				})
				.AssertAgainst(
					HttpResponseTests.CreatedWithId(BaseUri + "/api/cats/{0}", id => CreatedCatId = id)
				);

			Assert.AreNotEqual(Guid.Empty, CreatedCatId);
		}

		[Test]
		public void Step_02_00()
		{
			Http
				.Get("/api/cats/" + CreatedCatId.ToString("N"))
				.AssertAgainst(
					HttpResponseTests.JsonResult(new
					{
						id = CreatedCatId.ToString("N"),
						name = "Felix",
						purrPower = 8
					})
				);
		}

		[Test]
		public void Step_03_00()
		{
			Http
				.Put("/api/cats/" + CreatedCatId.ToString("N"), new
				{
					name = "Felix",
					purrPower = 12
				})
				.AssertAgainst(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public void Step_04_00()
		{
			Http
				.Get("/api/cats/" + CreatedCatId.ToString("N"))
				.AssertAgainst(
					HttpResponseTests.JsonResult(new
					{
						id = CreatedCatId.ToString("N"),
						name = "Felix",
						purrPower = 12
					})
				);
		}

		[Test]
		public void Step_05_00()
		{
			Http
				.Delete("/api/cats/" + CreatedCatId.ToString("N"))
				.AssertAgainst(
					HttpResponseTests.StatusCode(NoContent)
				);
		}
	}
}
