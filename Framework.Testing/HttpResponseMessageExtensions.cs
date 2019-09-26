using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Nap.Framework.Testing
{
	public static class HttpResponseMessageExtensions
	{
		public static async Task AssertResponse(this Task<HttpResponseMessage> @this, params HttpResponseTest[] tests)
		{
			HttpResponseMessage response = await @this;
			HttpResponseTestResult overallResult = await HttpResponseTests.Group(tests).Invoke(response);

			if (overallResult.Failed)
			{
				Assert.Fail(string.Join("\n", overallResult.Remarks
					.Prepend("[FAIL] Assertion on HTTP response failed. c(TnTu)")
					.Append(response.ToString())
					.Append(await response.Content.ReadAsStringAsync())
				));
			}
		}
	}
}
