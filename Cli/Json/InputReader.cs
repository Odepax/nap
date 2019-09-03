using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Nap.Cli.Definitions;

namespace Nap.Cli.Json
{
	public static class InputReader
	{
		public static IReadOnlyList<Resource> ReadFromJson(Stream stream)
		{
			using JsonDocument json = JsonDocument.Parse(stream);

			List<Resource> resources = new List<Resource>();

			foreach (JsonProperty resourceProperty in json.RootElement.EnumerateObject())
			{
				string resourceName = resourceProperty.Name;
				Dictionary<string, string> resourceFields = new Dictionary<string, string>();

				foreach (JsonProperty fieldProperty in resourceProperty.Value.EnumerateObject())
				{
					string fieldName = fieldProperty.Name;
					string fieldType = fieldProperty.Value.GetString();

					resourceFields.Add(fieldName, fieldType);
				}

				resources.Add(new Resource(resourceName, resourceFields));
			}

			return resources;
		}
	}
}
