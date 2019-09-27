using System;
using System.Diagnostics;
using System.Net.Http;

namespace Nap.Framework.Testing
{
	public sealed class ApiServer : IDisposable
	{
		public readonly ApiServerEndpoint Root;

		private readonly Process ServerProcess;

		public ApiServer(string exePath, string url)
		{
			Root = new ApiServerEndpoint(url, new HttpClient());

			ServerProcess = Process.Start(new ProcessStartInfo
			{
				FileName = exePath,
				Arguments = $"urls={ url }",
				UseShellExecute = false,
				CreateNoWindow = false
			});
		}

		public void Dispose()
		{
			ServerProcess.Kill(entireProcessTree: true);
			ServerProcess.Dispose();
		}

		public ApiServerEndpoint Select(string path) =>
			Root.Select(path);
	}
}
