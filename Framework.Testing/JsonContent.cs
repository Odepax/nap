using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Nap.Framework.Testing
{
	public sealed class JsonContent : StringContent
	{
		public JsonContent(object content) : base(
			JsonSerializer.Serialize(
				content,
				new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
					IgnoreNullValues = false,
					IgnoreReadOnlyProperties = false,
					WriteIndented = true
				}
			),
			Encoding.UTF8,
			Application.Json
		)
		{
		}
	}
}
