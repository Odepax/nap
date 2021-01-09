using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nap {
	/// <summary>
	/// Type-safe extensions for settings standard constraints when building types in input methods.
	/// </summary>
	public static class SetContraintFieldTypeBuilderExtensions {
		public static void SetMin(this IFieldTypeBuilder @this, int min) => @this.SetContraint(StdConstraints.MIN, min);
		public static void SetMin(this IFieldTypeBuilder @this, float min) => @this.SetContraint(StdConstraints.MIN, min);
		public static void SetMin(this IFieldTypeBuilder @this, DateTime min) => @this.SetContraint(StdConstraints.MIN, min);
		public static void SetMin(this IFieldTypeBuilder @this, TimeSpan min) => @this.SetContraint(StdConstraints.MIN, min);

		public static void SetMax(this IFieldTypeBuilder @this, int max) => @this.SetContraint(StdConstraints.MAX, max);
		public static void SetMax(this IFieldTypeBuilder @this, float max) => @this.SetContraint(StdConstraints.MAX, max);
		public static void SetMax(this IFieldTypeBuilder @this, DateTime max) => @this.SetContraint(StdConstraints.MAX, max);
		public static void SetMax(this IFieldTypeBuilder @this, TimeSpan max) => @this.SetContraint(StdConstraints.MAX, max);

		public static void SetMinIsInclusive(this IFieldTypeBuilder @this, bool minIsInclusive) => @this.SetContraint(StdConstraints.MINISINCLUSIVE, minIsInclusive);
		public static void SetMaxIsInclusive(this IFieldTypeBuilder @this, bool maxIsInclusive) => @this.SetContraint(StdConstraints.MAXISINCLUSIVE, maxIsInclusive);

		public static void SetExclusionMin(this IFieldTypeBuilder @this, int exclusionMin) => @this.SetContraint(StdConstraints.EXCLUSIONMIN, exclusionMin);
		public static void SetExclusionMin(this IFieldTypeBuilder @this, float exclusionMin) => @this.SetContraint(StdConstraints.EXCLUSIONMIN, exclusionMin);
		public static void SetExclusionMin(this IFieldTypeBuilder @this, DateTime exclusionMin) => @this.SetContraint(StdConstraints.EXCLUSIONMIN, exclusionMin);
		public static void SetExclusionMin(this IFieldTypeBuilder @this, TimeSpan exclusionMin) => @this.SetContraint(StdConstraints.EXCLUSIONMIN, exclusionMin);

		public static void SetExclusionMax(this IFieldTypeBuilder @this, int exclusionMax) => @this.SetContraint(StdConstraints.EXCLUSIONMAX, exclusionMax);
		public static void SetExclusionMax(this IFieldTypeBuilder @this, float exclusionMax) => @this.SetContraint(StdConstraints.EXCLUSIONMAX, exclusionMax);
		public static void SetExclusionMax(this IFieldTypeBuilder @this, TimeSpan exclusionMax) => @this.SetContraint(StdConstraints.EXCLUSIONMAX, exclusionMax);
		public static void SetExclusionMax(this IFieldTypeBuilder @this, DateTime exclusionMax) => @this.SetContraint(StdConstraints.EXCLUSIONMAX, exclusionMax);

		public static void SetExclusionMinIsInclusive(this IFieldTypeBuilder @this, bool exclusionMinIsInclusive) => @this.SetContraint(StdConstraints.EXCLUSIONMINISINCLUSIVE, exclusionMinIsInclusive);
		public static void SetExclusionMaxIsInclusive(this IFieldTypeBuilder @this, bool exclusionMaxIsInclusive) => @this.SetContraint(StdConstraints.EXCLUSIONMAXISINCLUSIVE, exclusionMaxIsInclusive);

		public static void SetDefault(this IFieldTypeBuilder @this, bool defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, int defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, float defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, char defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, string defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, DateTime defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);
		public static void SetDefault(this IFieldTypeBuilder @this, TimeSpan defaultValue) => @this.SetContraint(StdConstraints.DEFAULT, defaultValue);

		public static void SetIsOptional(this IFieldTypeBuilder @this, bool isOptional) => @this.SetContraint(StdConstraints.ISOPTIONAL, isOptional);

		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<int> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);
		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<float> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);
		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<char> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);
		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<string> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);
		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<DateTime> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);
		public static void SetAllowedValues(this IFieldTypeBuilder @this, IReadOnlyCollection<TimeSpan> allowedValues) => @this.SetContraint(StdConstraints.ALLOWEDVALUES, allowedValues);

		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<int> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);
		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<float> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);
		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<char> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);
		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<string> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);
		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<DateTime> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);
		public static void SetForbiddenValues(this IFieldTypeBuilder @this, IReadOnlyCollection<TimeSpan> forbiddenValues) => @this.SetContraint(StdConstraints.FORBIDDENVALUES, forbiddenValues);

		public static void SetAllowsEmpty(this IFieldTypeBuilder @this, bool allowsEmpty) => @this.SetContraint(StdConstraints.ALLOWSEMPTY, allowsEmpty);
		public static void SetAllowsDuplicates(this IFieldTypeBuilder @this, bool allowsDuplicates) => @this.SetContraint(StdConstraints.ALLOWSDUPLICATES, allowsDuplicates);

		public static void SetPattern(this IFieldTypeBuilder @this, Regex pattern) => @this.SetContraint(StdConstraints.PATTERN, pattern);
		public static void SetAllowsMultiline(this IFieldTypeBuilder @this, bool allowsMultiline) => @this.SetContraint(StdConstraints.ALLOWSMULTILINE, allowsMultiline);

		public static void SetSameAs(this IFieldTypeBuilder @this, string sameAsFieldName) => @this.SetContraint(StdConstraints.SAMEAS, sameAsFieldName);
		public static void SetNotSameAs(this IFieldTypeBuilder @this, string notSameAsFieldName) => @this.SetContraint(StdConstraints.NOTSAMEAS, notSameAsFieldName);

		public static void SetSelfReference(this IFieldTypeBuilder @this, bool selfReference) => @this.SetContraint(StdConstraints.SELFREFERENCE, selfReference);
	}
}
