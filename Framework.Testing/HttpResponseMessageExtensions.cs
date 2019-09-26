using System.Linq;
using System.Net.Http;
using NUnit.Framework;

namespace Nap.Framework.Testing
{
	public static class HttpResponseMessageExtensions
	{
		public static void AssertResponse(this HttpResponseMessage @this, params HttpResponseTest[] tests)
		{
			HttpResponseTestResult overallResult = HttpResponseTests.Group(tests).Invoke(@this);

			if (overallResult.Failed)
			{
				Assert.Fail(string.Join("\n", overallResult.Remarks
					.Prepend("[FAIL] Assertion on HTTP response failed. c(TnTu)")
					.Append(@this.ToString())
					.Append(@this.Content.ReadAsStringAsync().GetAwaiter().GetResult())
				));
			}
		}
	}
}
