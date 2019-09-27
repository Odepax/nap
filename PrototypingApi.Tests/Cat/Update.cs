using System;
using System.Threading.Tasks;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static Nap.Framework.Testing.HttpResponseTests;

namespace Nap.PrototypingApi.Tests.Cat
{
	public static class Update
	{
		[Test]
		public static async Task U()
		{
			Guid created = Guid.Empty;

			await Api.Endpoints.Cat
				.Post(new
				{
					name = "Felix",
					purrPower = 8
				})
				.AssertResponse(
					CreatedWithId(Api.Endpoints.Cat.Url + "/{0}", id => created = id)
				);

			await Api.Endpoints.Cat.Select(created.ToString())
				.Put(new
				{
					name = "Felix",
					purrPower = 2,
					isGrumpy = true
				})
				.AssertResponse(
					StatusCode(NoContent)
				);

			await Api.Endpoints.Cat.Select(created.ToString())
				.Get()
				.AssertResponse(
					JsonResult(new
					{
						name = "Felix",
						purrPower = 2,
						isGrumpy = true,
						id = created.ToString()
					})
				);

			await Api.Endpoints.Cat.Select(created.ToString())
				.Delete()
				.AssertResponse(
					StatusCode(NoContent)
				);
		}

		[Test]
		public static async Task U_404()
		{
			Guid target = new Guid(200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			await Api.Endpoints.Cat.Select(target.ToString())
				.Put(new
				{
					name = "Garfield",
					purrPower = 1,
					isGrumpy = false
				})
				.AssertResponse(
					StatusCode(NoContent)
				);

			await Api.Endpoints.Cat.Select(target.ToString())
				.Get()
				.AssertResponse(
					JsonResult(new
					{
						name = "Garfield",
						purrPower = 1,
						isGrumpy = false,
						id = target.ToString()
					})
				);

			await Api.Endpoints.Cat.Select(target.ToString())
				.Delete()
				.AssertResponse(
					StatusCode(NoContent)
				);
		}

		[Test]
		[Ignore("TODO: 1 invalid test / prop / validation rule + with several props")]
		public static async Task U_Invalid()
		{
		}

		[Test]
		[Ignore("TODO: determine which triggers first")]
		public static async Task U_404_Invalid()
		{
		}
	}
}
