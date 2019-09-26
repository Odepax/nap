using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using static System.Net.HttpStatusCode;
using static System.Net.Mime.MediaTypeNames;

namespace Nap.Framework.Testing
{
	public static class HttpResponseTests
	{
		public static HttpResponseTest StatusCode(HttpStatusCode statusCode) =>
			Factory(
				"Status code",
				response => response.StatusCode == statusCode,
				statusCode
			);

		public static HttpResponseTest ContentType(string mimeTypeName) =>
			Factory(
				"Content-Type",
				response => response.Content.Headers.ContentType.MediaType.Equals(mimeTypeName),
				mimeTypeName
			);

		public static HttpResponseTest TextContent(string text) =>
			Factory(
				"Text content",
				response => response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Equals(text),
				text
			);

		public static HttpResponseTest JsonContent(object json)
		{
			string expectedJson = JsonSerializer.Serialize(json, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
				IgnoreNullValues = false,
				IgnoreReadOnlyProperties = false,
				WriteIndented = true
			});

			return Factory(
				"Json content",
				response => response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Equals(expectedJson),
				expectedJson
			);
		}

		public static HttpResponseTest JsonResult(object json) =>
			Group(
				StatusCode(OK),
				ContentType(Application.Json),
				JsonContent(json)
			);

		public static HttpResponseTest CreatedWithId(string uriFormat, Action<Guid>? callback = null) =>
			Group(
				ContentType(Text.Plain),
				StatusCode(Created),
				response =>
				{
					if (!Guid.TryParse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), out Guid id))
					{
						return HttpResponseTestResult.Fail("Content is not a valid id.");
					}
					else if(id == Guid.Empty)
					{
						return HttpResponseTestResult.Fail("Content is a 'zero' id.");
					}
					else if (response.Headers.Location.ToString().Equals(string.Format(uriFormat, id.ToString())))
					{
						callback?.Invoke(id);

						return HttpResponseTestResult.Pass();
					}
					else
					{
						return HttpResponseTestResult.Fail("Location " + response.Headers.Location.ToString() + " does not match template URI with id.");
					}
				}
			);

		internal static HttpResponseTest Group(params HttpResponseTest[] tests) =>
			response =>
			{
				HttpResponseTestResult overallResult = HttpResponseTestResult.Pass();

				foreach (HttpResponseTest test in tests)
				{
					HttpResponseTestResult result = test.Invoke(response);

					overallResult.Passed = overallResult.Passed && result.Passed;
					overallResult.Remarks.AddRange(result.Remarks);
				}

				return overallResult;
			};

		private static HttpResponseTest Factory(string subject, Predicate<HttpResponseMessage> predicate, object expectedValue) =>
			response => predicate(response)
				? HttpResponseTestResult.Pass()
				: HttpResponseTestResult.Fail($"{ subject } is not { expectedValue }");
	}
}
