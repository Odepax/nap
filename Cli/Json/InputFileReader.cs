using System.Collections.Generic;
using System.IO;
using Nap.Cli.Definitions;

namespace Nap.Cli.Json
{
	public static class InputFileReader
	{
		public static IReadOnlyList<Resource> ReadFromJson(FileInfo file)
		{
			using Stream stream = file.OpenRead();

			return InputReader.ReadFromJson(stream);
		}
	}
}
