using System;
using System.Text;

namespace Nap {
	public sealed class CodeBuilder {
		readonly StringBuilder Code;
		readonly string IndentString;

		byte IndentLevel = 0;
		bool MustIndent = true;

		public CodeBuilder( /*                */ string indentString = "\t") : this(new StringBuilder(), indentString) {}
		public CodeBuilder(StringBuilder output, string indentString = "\t") {
			Code = output;
			IndentString = indentString;
		}

		public CodeBuilder Indent() {
			++IndentLevel;

			return this;
		}

		public CodeBuilder Unindent() {
			if (0 <= IndentLevel)
				--IndentLevel;

			return this;
		}

		public CodeBuilder AssertIndentLevel(int expected = 0) {
			if (IndentLevel != expected)
				throw new InvalidOperationException($"Indentation level is { IndentLevel } instead of expected { expected }.");

			return this;
		}

		/// <summary> Appends a single indentation. </summary>
		public CodeBuilder AppendIndent() {
			Code.Append(IndentString);

			return this;
		}

		/// <summary>
		/// Appends a new line.
		/// The next non-new-line append will append indentations up to the current indent level.
		/// </summary>
		public CodeBuilder AppendLine() {
			Code.AppendLine();

			MustIndent = true;

			return this;
		}

		public CodeBuilder Append(string value) {
			if (MustIndent) {
				MustIndent = false;

				for (var i = 0; i < IndentLevel; ++i)
					Code.Append(IndentString);
			}

			Code.Append(value);

			return this;
		}

		public override string ToString() => Code.ToString();

		//public int Length { get => Code.Length; set => Code.Length = value; }
		//public int Capacity { get => Code.Capacity; set => Code.Capacity = value; }
		//public int MaxCapacity => Code.MaxCapacity;

		//public char this[int index] { get => Code[index]; set => Code[index] = value; }

		//public CodeBuilder Append(byte value) => Append(value.ToString());
		//public CodeBuilder Append(uint value) => Append(value.ToString());
		//public CodeBuilder Append(ushort value) => Append(value.ToString());
		//public CodeBuilder Append(ulong value) => Append(value.ToString());
		//public CodeBuilder Append(sbyte value) => Append(value.ToString());
		//public CodeBuilder Append(short value) => Append(value.ToString());
		//public CodeBuilder Append(int value) => Append(value.ToString());
		//public CodeBuilder Append(long value) => Append(value.ToString());
		//public CodeBuilder Append(float value) => Append(value.ToString());
		//public CodeBuilder Append(double value) => Append(value.ToString());
		//public CodeBuilder Append(decimal value) => Append(value.ToString());
		//public CodeBuilder Append(bool value) => Append(value.ToString());
		//public CodeBuilder Append(char value) => Append(value.ToString());
		//public CodeBuilder Append(object? value) => Append(value?.ToString() ?? string.Empty);

		//public CodeBuilder AppendJoin<T>(IEnumerable<T> values, string separator = "", string prefix = "", string suffix = "", string fallback = "") {
		//	using var enumerator = values.GetEnumerator();

		//	if (enumerator.MoveNext()) {
		//		Append(prefix);
		//		Append(enumerator.Current);

		//		while (enumerator.MoveNext()) {
		//			Append(separator);
		//			Append(enumerator.Current);
		//		}

		//		Append(suffix);
		//	}

		//	else Append(fallback);

		//	return this;
		//}
	}
}
