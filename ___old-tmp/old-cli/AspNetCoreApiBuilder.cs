using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nap.CsGeneration;

namespace Nap
{
	public sealed class AspNetCoreApiBuilder : IApiPartBuilder
	{
		public void BuildApiPart(string apiName, IReadOnlyList<ApiResource> resources, DirectoryInfo destinationDirectory)
		{
			apiName = apiName.ToPascal();

			InvokeDotnet($@"new sln -n ""{ apiName }"" -o ""{ destinationDirectory.FullName }""");
			InvokeDotnet($@"new webapi -n ""{ apiName }"" -o ""{ destinationDirectory.Sub(apiName).FullName }""");
			InvokeDotnet($@"sln ""{ destinationDirectory.File(apiName, "sln").FullName }"" add ""{ destinationDirectory.Sub(apiName).File(apiName, "csproj").FullName }""");

			destinationDirectory.Sub(apiName).Sub("Controllers").Delete(recursive: true);
			destinationDirectory.Sub(apiName).File("WeatherForecast.cs").Delete();

			List<TypeInfo> resourceClasses = resources
				.Select(resource => resource.ToTypeInfo(apiName))
				.ToList();

			DirectoryInfo dataClassDirectory = destinationDirectory.Sub(apiName).CreateSubdirectory("Data");
			FileInfo dbContextClassFile = dataClassDirectory.File(apiName + "Context", "cs");

			BuildResourceClasses(dataClassDirectory, resourceClasses);
			BuildDbContextClass(dbContextClassFile, apiName, resourceClasses);
		}

		private static void InvokeDotnet(string arguments)
		{
			Console.WriteLine("[NAP] > dotnet " + arguments);

			Process
				.Start(new ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = arguments,
					UseShellExecute = false,
					CreateNoWindow = false
				})
				.WaitForExit();
		}

		private static void BuildResourceClasses(DirectoryInfo dataClassDirectory, List<TypeInfo> resourceClasses)
		{
			foreach (TypeInfo @class in resourceClasses)
			{
				using StreamWriter classFile = dataClassDirectory.File(@class.Name, "cs").CreateText();
				using CodeUnit classFileContent = CodeUnit.New();

				classFileContent.WriteType(@class);
				classFile.Write(classFileContent.ToString());
			}
		}

		private static void BuildDbContextClass(FileInfo dbContextClassFile, string apiName, List<TypeInfo> resourceClasses)
		{
			using StreamWriter classFile = dbContextClassFile.CreateText();
			using CodeUnit classFileContent = CodeUnit.New();

			classFileContent.WriteDbContext(apiName, resourceClasses);
			classFile.Write(classFileContent.ToString());
		}

		//public sealed class RestControllerBuilder : IApiPartBuilder
		//{
		//	public void BuildApiPart(string apiName, IList<ApiResource> resources, DirectoryInfo destinationDirectory)
		//	{
		//	}
		//}
	}
}
