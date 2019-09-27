using System;
using System.Threading.Tasks;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static Nap.Framework.Testing.HttpResponseTests;

namespace Nap.PrototypingApi.Tests.Cat
{
	public static class Create
	{
		[Test]
		public static async Task C()
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
				.Get()
				.AssertResponse(
					JsonResult(new
					{
						name = "Felix",
						purrPower = 8,
						isGrumpy = false,
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
		[Ignore("TODO: 1 invalid test / prop / validation rule + with several props")]
		public static async Task C_Invalid()
		{
		}
	}
}
