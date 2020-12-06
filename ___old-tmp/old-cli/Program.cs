using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Nap.CsGeneration;
using static System.Reflection.TypeAttributes;

namespace Nap
{
	public static class Program
	{
		private static string HelpText(string message = "") => new StringBuilder()
			.AppendLine("")
			.AppendLine("Nap API Builder")
			.AppendLine("====")
			.Append(message.Length == 0 ? string.Empty : ("\n(*n*)\n" + message + "\n"))
			.AppendLine("")
			.AppendLine("Usage:")
			.AppendLine("> nap <COMMAND> <PARAMETERS>")
			.AppendLine("")
			.AppendLine("Command: build")
			.AppendLine("Parameters:")
			.AppendLine("   -s <PATH>")
			.AppendLine("   --source-file <PATH>")
			.AppendLine("      Path to the JSON source file.")
			.AppendLine("      Must exist.")
			.AppendLine("      Must be readable.")
			.AppendLine("      Regular files only.")
			.AppendLine("")
			.AppendLine("   -d <PATH>")
			.AppendLine("   --destination-directory <PATH>")
			.AppendLine("      Path to the directory where the API project will be generated.")
			.AppendLine("      Must be writable.")
			.AppendLine("      Will be created if it does not exist.")
			.AppendLine("      Regular directories only.")
			.AppendLine("")
			.ToString();

		public static void Main(string[] args)
		{
			(string command, IReadOnlyDictionary<string, string> parameters) = ConvertFromCommandLineArguments(args);

			if (!command.Equals("build"))
			{
				Console.Error.Write(HelpText("The only supported command is 'build'."));
				Environment.Exit(1);
			}

			string? sourceFilePath = parameters.GetValueOrDefault("source-file")
				?? parameters.GetValueOrDefault("s");

			string? destinationDirectoryPath = parameters.GetValueOrDefault("destination-directory")
				?? parameters.GetValueOrDefault("d");

			if (sourceFilePath is null)
			{
				Console.Error.Write(HelpText("Missing 'source-file' parameter."));
				Environment.Exit(3);
			}

			if (destinationDirectoryPath is null)
			{
				Console.Error.Write(HelpText("Missing 'destination-directory' parameter."));
				Environment.Exit(3);
			}

			FileInfo sourceFile = new FileInfo(sourceFilePath);
			DirectoryInfo destinationDirectory = new DirectoryInfo(destinationDirectoryPath);

			if (!sourceFile.Exists)
			{
				Console.Error.Write(HelpText("The file specified by the 'source-file' parameter does not exist."));
				Environment.Exit(4);
			}

			if (destinationDirectory.Exists)
			{
				destinationDirectory.Delete(recursive: true);
				destinationDirectory.Create();
			}

			BuildNapApi(
				sourceFile,
				destinationDirectory,
				new IApiResourceReader[] { new CustomJsonResourceReader() },
				new IApiPartBuilder[] { new AspNetCoreApiBuilder() }
			);
		}

		private static void BuildNapApi(FileInfo sourceFile, DirectoryInfo destinationDirectory, IEnumerable<IApiResourceReader> apiResourceReaders, IEnumerable<IApiPartBuilder> apiPartBuilders)
		{
			(string apiName, IReadOnlyList<ApiResource> resources) = apiResourceReaders
				.First(reader => reader.CanRead(sourceFile))
				.ReadResource(sourceFile);

			foreach (IApiPartBuilder builder in apiPartBuilders)
				builder.BuildApiPart(apiName, resources, destinationDirectory);
		}

		private static (string Command, IReadOnlyDictionary<string, string> Parameters) ConvertFromCommandLineArguments(string[] args)
		{
			string command = args.FirstOrDefault() ?? string.Empty;
			Dictionary<string, string> parameters = new Dictionary<string, string>();

			for (int i = 1; i < args.Length; i += 2)
			{
				if (args[i + 0].StartsWith('-'))
				{
					parameters[args[i + 0].TrimStart('-')] = args[i + 1];
				}
				else
				{
					Console.Error.Write(HelpText("Parameters must all be declared starting with a '-'."));
					Environment.Exit(1);
				}
			}

			return (command, parameters);
		}
	}
}
