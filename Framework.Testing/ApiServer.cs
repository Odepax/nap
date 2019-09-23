using System;
using System.Net.Http;

namespace Nap.Framework.Testing
{
	public sealed class ApiServer
	{
		public readonly ApiServerEndpoint Root;

		public ApiServer(string url)
		{
			Root = new ApiServerEndpoint(url, new HttpClient());
		}

		public ApiServerEndpoint Select(string path) =>
			Root.Select(path);
	}
}
