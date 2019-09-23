using Nap.Framework.Testing;

namespace Nap.PrototypingApi.Tests
{
	internal static class Api
	{
		private static readonly ApiServer Server = new ApiServer("http://localhost:52806");

		internal static readonly ServerEndpoints Endpoints = new ServerEndpoints
		{
			Cat = Server.Select("/api/cats")
		};

		internal struct ServerEndpoints
		{
			internal ApiServerEndpoint Cat;
		}
	}
}
