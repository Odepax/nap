using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Nap.CsGeneration.Tests.Tools;
using NUnit.Framework;

namespace Nap.CsGeneration.Tests
{
	public static class HttpResponseAssertions
	{
		public static void AssertAgainst(this HttpResponseMessage @this, params HttpResponseTest[] tests)
		{
			HttpResponseTestResult result = @this.TestAgainst(tests);

			if (result.Failed)
			{
				Assert.Fail(string.Join("\n", result.Remarks
					.Prepend("/!\\ Assertion on HTTP response failed")
					.Append(@this.ToString())
					.Append(@this.Content.ReadAsStringAsync().GetAwaiter().GetResult())
				));
			}
		}
	}
}
