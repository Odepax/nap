using System.Collections.Generic;

namespace Nap.CsGeneration.Tests.Tools
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
		public static HttpResponseTestResult Pass(string message) =>
			new HttpResponseTestResult { Status = HttpResponseTestResultStatus.Passed, Remarks = { message } };

		public static HttpResponseTestResult Fail(string message) =>
			new HttpResponseTestResult { Status = HttpResponseTestResultStatus.Failed, Remarks = { message } };

		public HttpResponseTestResultStatus Status { get; set; }
		public ICollection<string> Remarks { get; } = new List<string>();

		public bool Passed => Status == HttpResponseTestResultStatus.Passed || Status == HttpResponseTestResultStatus.PassedWithWarnings;
		public bool Failed => Status == HttpResponseTestResultStatus.Failed;
	}
}
