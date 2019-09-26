using System.Net.Http;
using System.Threading.Tasks;

namespace Nap.Framework.Testing
{
	public delegate Task<HttpResponseTestResult> HttpResponseTest(HttpResponseMessage response);
}
