using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
				async response => (await response.Content.ReadAsStringAsync()).Equals(text),
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
				async response => (await response.Content.ReadAsStringAsync()).Equals(expectedJson),
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
				async response =>
				{
					if (!Guid.TryParse(await response.Content.ReadAsStringAsync(), out Guid id))
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
			async response =>
			{
				HttpResponseTestResult overallResult = HttpResponseTestResult.Pass();

				foreach (HttpResponseTestResult result in await Task.WhenAll(tests.Select(test => test.Invoke(response))))
				{
					overallResult.Passed = overallResult.Passed && result.Passed;
					overallResult.Remarks.AddRange(result.Remarks);
				}

				return overallResult;
			};

		private static HttpResponseTest Factory(string subject, Predicate<HttpResponseMessage> predicate, object expectedValue) =>
			response => Task.FromResult(
				predicate(response)
					? HttpResponseTestResult.Pass()
					: HttpResponseTestResult.Fail($"{ subject } is not { expectedValue }")
			);

		private static HttpResponseTest Factory(string subject, Func<HttpResponseMessage, Task<bool>> predicate, object expectedValue) =>
			response => predicate(response).ContinueWith(
				it => it.Result
					? HttpResponseTestResult.Pass()
					: HttpResponseTestResult.Fail($"{ subject } is not { expectedValue }")
			);
	}
}
