using System.Collections.Generic;

namespace Nap.Framework.Testing
{
	public sealed class HttpResponseTestResult
	{
		public static HttpResponseTestResult Pass() =>
			new HttpResponseTestResult { Passed = true };

		public static HttpResponseTestResult Fail(string remark) =>
			new HttpResponseTestResult { Passed = false, Remarks = { remark } };

		public bool Passed { get; set; }
		public bool Failed => !Passed;

		public List<string> Remarks { get; } = new List<string>();

		private HttpResponseTestResult()
		{
		}
	}
}
