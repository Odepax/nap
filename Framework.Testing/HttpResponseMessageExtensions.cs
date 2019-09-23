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

			switch (overallResult.Status)
			{
				case HttpResponseTestResultStatus.Passed:
					Assert.Pass(string.Join("\n", overallResult.Remarks
						.Prepend("[PASS] Assertion on HTTP response passed. t(-.-t)")
						.Append(@this.ToString())
						.Append(@this.Content.ReadAsStringAsync().GetAwaiter().GetResult())
					));
					break;

				case HttpResponseTestResultStatus.PassedWithWarnings:
					Assert.Warn(string.Join("\n", overallResult.Remarks
						.Prepend("[WARN] Assertion on HTTP response passed with warnings. (9*O*)9")
						.Append(@this.ToString())
						.Append(@this.Content.ReadAsStringAsync().GetAwaiter().GetResult())
					));
					break;

				default:
					Assert.Fail(string.Join("\n", overallResult.Remarks
						.Prepend("[FAIL] Assertion on HTTP response failed. c(TnTu)")
						.Append(@this.ToString())
						.Append(@this.Content.ReadAsStringAsync().GetAwaiter().GetResult())
					));
					break;
			}
		}
	}
}
