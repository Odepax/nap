using System.Net.Http;
using System.Threading;

namespace Nap.CsGeneration.Tests.Tools
{
	public static class HttpMessageInvokerExtensions
	{
		public static HttpResponseMessage Send(this HttpMessageInvoker @this, HttpRequestMessage request) =>
			@this.SendAsync(request, CancellationToken.None).GetAwaiter().GetResult();

		public static HttpResponseMessage Get(this HttpMessageInvoker @this, string uri) =>
			@this.Send(new HttpRequestMessage(HttpMethod.Get, uri));

		public static HttpResponseMessage Post(this HttpMessageInvoker @this, string uri, object json) =>
			@this.Send(new HttpRequestMessage(HttpMethod.Post, uri) { Content = new JsonContent(json) });

		public static HttpResponseMessage Put(this HttpMessageInvoker @this, string uri, object json) =>
			@this.Send(new HttpRequestMessage(HttpMethod.Put, uri) { Content = new JsonContent(json) });

		public static HttpResponseMessage Delete(this HttpMessageInvoker @this, string uri) =>
			@this.Send(new HttpRequestMessage(HttpMethod.Delete, uri));
	}
}
