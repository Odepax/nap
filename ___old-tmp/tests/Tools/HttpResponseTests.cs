using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using static System.Net.HttpStatusCode;
using static System.Net.Mime.MediaTypeNames;

namespace Nap.CsGeneration.Tests.Tools
{
	public static class HttpResponseTests
	{
		public static HttpResponseTestResult TestAgainst(this HttpResponseMessage @this, params HttpResponseTest[] tests) =>
			TestAgainst(@this, tests as IEnumerable<HttpResponseTest>);

		public static HttpResponseTestResult TestAgainst(this HttpResponseMessage @this, IEnumerable<HttpResponseTest> tests)
		{
			HttpResponseTestResult overallResult = new HttpResponseTestResult { Status = HttpResponseTestResultStatus.Passed };

			foreach (HttpResponseTest test in tests)
			{
				HttpResponseTestResult result = test.Invoke(@this);
				
				foreach (string remark in result.Remarks)
				{
					overallResult.Remarks.Add(remark);
				}

				if (result.Failed)
				{
					overallResult.Status = HttpResponseTestResultStatus.Failed;

					return overallResult;
				}
			}

			return overallResult;
		}

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
			response => TestAgainst(
				response,
				StatusCode(OK),
				ContentType(Application.Json),
				JsonContent(json)
			);

		public static HttpResponseTest CreatedWithId(string uriFormat, Action<Guid>? callback = null) =>
			response => TestAgainst(
				response,
				ContentType(Text.Plain),
				StatusCode(Created),
				LocationAndContentWithId(uriFormat, callback)
			);

		private static HttpResponseTest LocationAndContentWithId(string uriFormat, Action<Guid>? callback = null) =>
			response =>
			{
				if (!Guid.TryParse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), out Guid id))
				{
					return HttpResponseTestResult.Fail("Content is not a valid id.");
				}
				else if (response.Headers.Location.ToString().Equals(string.Format(uriFormat, id.ToString("N"))))
				{
					callback?.Invoke(id);

					return HttpResponseTestResult.Pass("Created with id " + id.ToString("N"));
				}
				else
				{
					return HttpResponseTestResult.Fail("Location " + response.Headers.Location.ToString() + " does not match template URI with id.");
				}
			};

		private static HttpResponseTest Factory(string subject, Predicate<HttpResponseMessage> predicate, object value) =>
			response => predicate(response)
				? HttpResponseTestResult.Pass(subject + " is " + value.ToString())
				: HttpResponseTestResult.Fail(subject + " is not " + value.ToString());
	}
}
