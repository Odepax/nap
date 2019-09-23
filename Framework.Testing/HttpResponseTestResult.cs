using System.Collections.Generic;

namespace Nap.Framework.Testing
{
	public enum HttpResponseTestResultStatus
	{
		None,
		Passed,
		PassedWithWarnings,
		Failed
	}
	
	public sealed class HttpResponseTestResult
	{
		public static HttpResponseTestResult Pass() =>
			new HttpResponseTestResult { Status = HttpResponseTestResultStatus.Passed };

		public static HttpResponseTestResult Warn(string remark) =>
			new HttpResponseTestResult { Status = HttpResponseTestResultStatus.PassedWithWarnings, Remarks = { remark } };

		public static HttpResponseTestResult Fail(string remark) =>
			new HttpResponseTestResult { Status = HttpResponseTestResultStatus.Failed, Remarks = { remark } };

		public HttpResponseTestResultStatus Status { get; set; }
		public List<string> Remarks { get; } = new List<string>();

		public bool Passed => Status == HttpResponseTestResultStatus.Passed || Status == HttpResponseTestResultStatus.PassedWithWarnings;
		public bool Failed => Status == HttpResponseTestResultStatus.Failed;

		private HttpResponseTestResult()
		{
		}
	}
}
