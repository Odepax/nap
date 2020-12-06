using System.Text.RegularExpressions;

namespace Nap
{
	public static class StringExtensions
	{
		public static string ToAgnostic(this string @this) =>
			Regex.Replace(
				Regex.Replace(@this, "[A-Z]", match => ' ' + match.Groups[0].Value).ToLower(),
				"[^a-z]+",
				" "
			).Trim();

		public static string ToPascal(this string @this) =>
			char.ToUpper(@this[0]) + Regex.Replace(@this.Substring(1), " ([a-z])", match => match.Groups[1].Value.ToUpper());

		public static string Pluralize(this string @this) =>
			@this.EndsWith("y", System.StringComparison.OrdinalIgnoreCase)
				? @this.Substring(0, @this.Length - 1) + "ies"
				:
			@this.EndsWith("s", System.StringComparison.OrdinalIgnoreCase)
				? @this + "es"
				: @this + "s";
	}
}
