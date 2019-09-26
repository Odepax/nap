using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<HttpResponseMessage> Send(HttpRequestMessage request) =>
			Http.SendAsync(request, CancellationToken.None);

		public Task<HttpResponseMessage> Get() =>
			Send(new HttpRequestMessage(HttpMethod.Get, Url));

		public Task<HttpResponseMessage> Post(object json) =>
			Send(new HttpRequestMessage(HttpMethod.Post, Url) { Content = new JsonContent(json) });

		public Task<HttpResponseMessage> Put(object json) =>
			Send(new HttpRequestMessage(HttpMethod.Put, Url) { Content = new JsonContent(json) });

		public Task<HttpResponseMessage> Delete() =>
			Send(new HttpRequestMessage(HttpMethod.Delete, Url));
	}
}
