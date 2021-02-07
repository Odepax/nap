using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Nap {
	/// <summary>
	/// Type-safe extensions for retrieving built-in metadata from resource fields' types.
	/// </summary>
	public static class TryGetMetaFieldTypeExtensions {
		public static bool TryGetMeta<T>(this FieldType @this, string key, [NotNullWhen(true), NotNullIfNotNull("@default")] out T? @out, T? @default = default) {
			if (@this.Meta.TryGetValue(key, out var value) && value is T typedValue) {
				@out = typedValue;
				return true;
			}

			else {
				@out = @default;
				return false;
			}
		}

		// Data
		// ----

		public static bool TryGetSameAs(this FieldType @this, [NotNullWhen(true)] out string? @out) => TryGetMeta(@this, NapBuiltInMeta.SameAs, out @out);
		public static bool TryGetNotSameAs(this FieldType @this, [NotNullWhen(true)] out string? @out) => TryGetMeta(@this, NapBuiltInMeta.NotSameAs, out @out);

		// Template
		// ----

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this TemplateType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Resource
		// ----

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this ResourceType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		/// <param name="out"> Defaults to <see cref="SelfReference.Allow"/>. </param>
		public static bool TryGetSelfReference(this ResourceType @this, out SelfReference @out) => TryGetMeta(@this, NapBuiltInMeta.SelfReference, out @out, SelfReference.Allow);

		// Bool
		// ----

		public static bool TryGetDefault(this BoolType @this, [NotNullWhen(true)] out bool? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this BoolType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Int
		// ----

		public static bool TryGetAllowedValues(this IntType @this, out IReadOnlyCollection<int> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<int>.Empty);
		public static bool TryGetForbiddenValues(this IntType @this, out IReadOnlyCollection<int> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<int>.Empty);

		public static bool TryGetMin(this IntType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this IntType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this IntType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this IntType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this IntType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this IntType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Float
		// ----

		public static bool TryGetAllowedValues(this FloatType @this, out IReadOnlyCollection<float> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<float>.Empty);
		public static bool TryGetForbiddenValues(this FloatType @this, out IReadOnlyCollection<float> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<float>.Empty);

		public static bool TryGetMin(this FloatType @this, [NotNullWhen(true)] out float? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this FloatType @this, [NotNullWhen(true)] out float? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this FloatType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this FloatType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this FloatType @this, [NotNullWhen(true)] out float? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this FloatType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Char
		// ----

		public static bool TryGetAllowedValues(this CharType @this, out IReadOnlyCollection<char> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<char>.Empty);
		public static bool TryGetForbiddenValues(this CharType @this, out IReadOnlyCollection<char> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<char>.Empty);

		public static bool TryGetDefault(this CharType @this, [NotNullWhen(true)] out char? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this CharType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// String
		// ----

		public static bool TryGetAllowedValues(this StringType @this, out IReadOnlyCollection<string> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<string>.Empty);
		public static bool TryGetForbiddenValues(this StringType @this, out IReadOnlyCollection<string> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<string>.Empty);
		public static bool TryGetPattern(this StringType @this, [NotNullWhen(true)] out Regex? @out) => TryGetMeta(@this, NapBuiltInMeta.Pattern, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetAllowEmpty(this StringType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowEmpty, out @out, true);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetAllowMultiline(this StringType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowMultiline, out @out, false);

		public static bool TryGetMin(this StringType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this StringType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this StringType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this StringType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this StringType @this, [NotNullWhen(true)] out string? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this StringType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Date
		// ----

		public static bool TryGetAllowedValues(this DateType @this, out IReadOnlyCollection<DateTime> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<DateTime>.Empty);
		public static bool TryGetForbiddenValues(this DateType @this, out IReadOnlyCollection<DateTime> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<DateTime>.Empty);

		public static bool TryGetMin(this DateType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this DateType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this DateType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this DateType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this DateType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this DateType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Datetime
		// ----

		public static bool TryGetAllowedValues(this DatetimeType @this, out IReadOnlyCollection<DateTime> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<DateTime>.Empty);
		public static bool TryGetForbiddenValues(this DatetimeType @this, out IReadOnlyCollection<DateTime> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<DateTime>.Empty);

		public static bool TryGetMin(this DatetimeType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this DatetimeType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this DatetimeType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this DatetimeType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this DatetimeType @this, [NotNullWhen(true)] out DateTime? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this DatetimeType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Duration
		// ----

		public static bool TryGetAllowedValues(this DurationType @this, out IReadOnlyCollection<TimeSpan> @out) => TryGetMeta(@this, NapBuiltInMeta.AllowedValues, out @out, ImmutableList<TimeSpan>.Empty);
		public static bool TryGetForbiddenValues(this DurationType @this, out IReadOnlyCollection<TimeSpan> @out) => TryGetMeta(@this, NapBuiltInMeta.ForbiddenValues, out @out, ImmutableList<TimeSpan>.Empty);

		public static bool TryGetMin(this DurationType @this, [NotNullWhen(true)] out TimeSpan? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this DurationType @this, [NotNullWhen(true)] out TimeSpan? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this DurationType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this DurationType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		public static bool TryGetDefault(this DurationType @this, [NotNullWhen(true)] out TimeSpan? @out) => TryGetMeta(@this, NapBuiltInMeta.Default, out @out);

		/// <param name="out"> Defaults to <c>false</c>. </param>
		public static bool TryGetIsOptional(this DurationType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.IsOptional, out @out, false);

		// Set
		// ----

		public static bool TryGetAllowDuplicates(this SetType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowDuplicates, out @out, false);
		public static bool TryGetAllowEmpty(this SetType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowEmpty, out @out, true);

		public static bool TryGetMin(this SetType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this SetType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this SetType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this SetType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		// List
		// ----

		public static bool TryGetAllowDuplicates(this ListType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowDuplicates, out @out, true);
		public static bool TryGetAllowEmpty(this ListType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowEmpty, out @out, true);

		public static bool TryGetMin(this ListType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this ListType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this ListType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this ListType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);

		// Map
		// ----

		public static bool TryGetAllowDuplicates(this MapType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowDuplicates, out @out, true);
		public static bool TryGetAllowEmpty(this MapType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.AllowEmpty, out @out, true);

		public static bool TryGetMin(this MapType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Min, out @out);
		public static bool TryGetMax(this MapType @this, [NotNullWhen(true)] out int? @out) => TryGetMeta(@this, NapBuiltInMeta.Max, out @out);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMinIsInclusive(this MapType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MinIsInclusive, out @out, true);

		/// <param name="out"> Defaults to <c>true</c>. </param>
		public static bool TryGetMaxIsInclusive(this MapType @this, out bool @out) => TryGetMeta(@this, NapBuiltInMeta.MaxIsInclusive, out @out, true);
	}
}
