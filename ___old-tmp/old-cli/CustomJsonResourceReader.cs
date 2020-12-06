using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Nap
{
	public sealed class CustomJsonResourceReader : IApiResourceReader
	{
		public bool CanRead(FileInfo sourceFile)
		{
			return sourceFile.Extension.EndsWith("json") && 0 < sourceFile.Length;
		}

		public (string ApiName, IReadOnlyList<ApiResource> Resources) ReadResource(FileInfo sourceFile)
		{
			using Stream stream = sourceFile.OpenRead();
			using JsonDocument document = JsonDocument.Parse(stream);

			IReadOnlyList<ApiResource> resources = document.RootElement.EnumerateArray()
				.Select(ConvertApiResource)
				.ToList();

			string apiName = sourceFile.Name.ToAgnostic();

			return (apiName, resources);
		}

		private static ApiResource ConvertApiResource(JsonElement input) =>
			new ApiResource(
				input.GetProperty("name").GetString(),
				input.GetProperty("fields").EnumerateArray()
					.Select(ConvertApiResourceField),
				new ApiResourceAnnotation[0]
			);
		private static ApiResourceField ConvertApiResourceField(JsonElement input) =>
			new ApiResourceField(
				input.GetProperty("name").GetString(),
				ConvertApiResourceFieldType(input.GetProperty("type")),
				new ApiResourceAnnotation[0]
			);
		private static ApiResourceFieldType ConvertApiResourceFieldType(JsonElement input) =>
			new ApiResourceFieldType(
				input.GetProperty("name").GetString(),
				new ApiResourceFieldType[0]
			);
	}
}
