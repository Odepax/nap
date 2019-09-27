using Nap.Framework.Testing;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.All)]

namespace Nap.PrototypingApi.Tests
{
	[SetUpFixture]
	internal static class Api
	{
		private static readonly ApiServer Server = new ApiServer(
			"../../../../PrototypingApi/bin/Debug/netcoreapp3.0/Nap.PrototypingApi.exe",
			"http://localhost:8080"
		);

		internal static readonly ServerEndpoints Endpoints = new ServerEndpoints
		{
			Cat = Server.Select("/api/cats")
		};

		internal struct ServerEndpoints
		{
			internal ApiServerEndpoint Cat;
		}

		[OneTimeTearDown]
		public static void OnStop()
		{
			Server.Dispose();
		}
	}
}
