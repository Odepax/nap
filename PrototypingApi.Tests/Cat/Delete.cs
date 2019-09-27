using System;
using System.Threading.Tasks;
using Nap.Framework.Testing;
using NUnit.Framework;
using static System.Net.HttpStatusCode;
using static Nap.Framework.Testing.HttpResponseTests;

namespace Nap.PrototypingApi.Tests.Cat
{
	public static class Delete
	{
		[Test]
		public static void D()
		{
			Assert.Pass("Already tested through other cases.");
		}

		[Test]
		public static async Task D_404()
		{
			Guid nonExisting = new Guid(404, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			await Api.Endpoints.Cat.Select(nonExisting.ToString())
				.Delete()
				.AssertResponse(
					StatusCode(NoContent)
				);
		}
	}
}
