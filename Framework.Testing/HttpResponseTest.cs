using System.Net.Http;

namespace Nap.Framework.Testing
{
	public delegate HttpResponseTestResult HttpResponseTest(HttpResponseMessage response);
}
