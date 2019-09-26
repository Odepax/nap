using System;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;

namespace Nap.PrototypingApi.Tests.Cat
{
	[TestFixture]
	[Parallelizable(ParallelScope.Children)]
	public static class CrudScenario
	{
		[Test]
		[NonParallelizable]
		public static void Step_00_00_Ra()
		{
			Api.Endpoints.Cat
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new object[0])
				);
		}

		[Test]
		public static void Step_01_00_CR_D()
		{
			Guid created = Guid.Empty;

			Api.Endpoints.Cat
				.Post(new
				{
					name = "Felix",
					purrPower = 8
				})
				.AssertResponse(
					HttpResponseTests.CreatedWithId(Api.Endpoints.Cat.Url + "/{0}", id => created = id)
				);

			Api.Endpoints.Cat.Select(created.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Felix",
						purrPower = 8,
						isGrumpy = false,
						id = created.ToString()
					})
				);

			Api.Endpoints.Cat.Select(created.ToString())
				.Delete()
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public static void Step_02_00_C_UR_D()
		{
			Guid created = Guid.Empty;

			Api.Endpoints.Cat
				.Post(new
				{
					name = "Felix",
					purrPower = 8
				})
				.AssertResponse(
					HttpResponseTests.CreatedWithId(Api.Endpoints.Cat.Url + "/{0}", id => created = id)
				);

			Api.Endpoints.Cat.Select(created.ToString())
				.Put(new
				{
					name = "Felix",
					purrPower = 2,
					isGrumpy = true
				})
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);

			Api.Endpoints.Cat.Select(created.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Felix",
						purrPower = 2,
						isGrumpy = true,
						id = created.ToString()
					})
				);

			Api.Endpoints.Cat.Select(created.ToString())
				.Delete()
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}

		[Test]
		public static void Step_03_00_UcR_D()
		{
			Guid target = new Guid(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			Api.Endpoints.Cat.Select(target.ToString())
				.Put(new
				{
					name = "Garfield",
					purrPower = 1,
					isGrumpy = false
				})
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);

			Api.Endpoints.Cat.Select(target.ToString())
				.Get()
				.AssertResponse(
					HttpResponseTests.JsonResult(new
					{
						name = "Garfield",
						purrPower = 1,
						isGrumpy = false,
						id = target.ToString()
					})
				);

			Api.Endpoints.Cat.Select(target.ToString())
				.Delete()
				.AssertResponse(
					HttpResponseTests.StatusCode(NoContent)
				);
		}
	}
}
