using System;
using System.Net.Http;
using System.Threading;

namespace Nap.Framework.Testing
{
	public struct ApiServerEndpoint
	{
		public readonly Uri Url;

		private readonly HttpMessageInvoker Http;

		internal ApiServerEndpoint(string path, HttpMessageInvoker http)
		{
			Url = new Uri(path);
			Http = http;
		}

		public ApiServerEndpoint Select(string path) =>
			new ApiServerEndpoint(Url.AbsoluteUri.TrimEnd('/') + '/' + path.TrimStart('/'), Http);

		private HttpResponseMessage Send(HttpRequestMessage request) =>
			Http.SendAsync(request, CancellationToken.None).GetAwaiter().GetResult();

		public HttpResponseMessage Get() =>
			Send(new HttpRequestMessage(HttpMethod.Get, Url));

		public HttpResponseMessage Post(object json) =>
			Send(new HttpRequestMessage(HttpMethod.Post, Url) { Content = new JsonContent(json) });

		public HttpResponseMessage Put(object json) =>
			Send(new HttpRequestMessage(HttpMethod.Put, Url) { Content = new JsonContent(json) });

		public HttpResponseMessage Delete() =>
			Send(new HttpRequestMessage(HttpMethod.Delete, Url));
	}
}
