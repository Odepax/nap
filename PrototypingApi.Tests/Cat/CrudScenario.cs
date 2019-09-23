using System;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static System.Net.Mime.MediaTypeNames;

namespace Nap.PrototypingApi.Tests.Cat
{
	[TestFixture]
	[SingleThreaded]
	public class CrudScenario
	{
		private static Api.ServerEndpoints Endpoints => Api.Endpoints;

		private static Guid CreatedId = Guid.Empty;
		private static readonly Guid SetId = new Guid(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		[Test]
		public void Step_00_00_R()
		{
			Endpoints.Cat
				.Get()
				.AssertResponse(
					HttpResponseTests.StatusCode(OK),
					HttpResponseTests.ContentType(Application.Json),
					HttpResponseTests.JsonContent(new object[0])
				);
		}

		[Test]
		public void Step_01_00_C()
		{
			Endpoints.Cat
				.Post(new
				{
					name = "Felix",
					purrPower = 8
				})
				.AssertResponse(
					HttpResponseTests.CreatedWithId(Endpoints.Cat.Url + "/{0}", id => CreatedId = id)
				);

			Assert.AreNotEqual(Guid.Empty, CreatedId);
		}

		[Test]
		public void Step_02_00_R()
		{
			Endpoints.Cat
				.Select(CreatedId.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Felix",
						purrPower = 8,
						isGrumpy = false,
						id = CreatedId.ToString()
					})
				);
		}

		[Test]
		public void Step_03_00_U()
		{
			Endpoints.Cat
				.Select(CreatedId.ToString())
				.Put(new
				{
					name = "Felix",
					purrPower = 2,
					isGrumpy = true
				})
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public void Step_04_00_R()
		{
			Endpoints.Cat
				.Select(CreatedId.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Felix",
						purrPower = 2,
						isGrumpy = true,
						id = CreatedId.ToString()
					})
				);
		}

		[Test]
		public void Step_05_00_D()
		{
			Endpoints.Cat
				.Select(CreatedId.ToString())
				.Delete()
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public void Step_06_00_C2()
		{
			Endpoints.Cat
				.Select(SetId.ToString())
				.Put(new
				{
					name = "Garfield",
					purrPower = 1,
					isGrumpy = false
				})
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public void Step_07_00_R2()
		{
			Endpoints.Cat
				.Select(SetId.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Garfield",
						purrPower = 1,
						isGrumpy = false,
						id = SetId.ToString()
					})
				);
		}

		[Test]
		public void Step_08_00_D2()
		{
			Endpoints.Cat
				.Select(SetId.ToString())
				.Delete()
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public void Step_0X_00_R()
		{
			Endpoints.Cat
				.Get()
				.AssertResponse(
					HttpResponseTests.StatusCode(OK),
					HttpResponseTests.ContentType(Application.Json),
					HttpResponseTests.JsonContent(new object[0])
				);
		}
	}
}
