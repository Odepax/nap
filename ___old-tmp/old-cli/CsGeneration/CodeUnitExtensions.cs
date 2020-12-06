using System;

namespace Nap.CsGeneration
{
	public static class CodeUnitExtensions
	{
		public static void WriteNamespace(this CodeUnit @this, string name, Action<CodeUnit> block)
		{
			@this.WriteChunkedLine("namespace ", name);
			@this.WriteBlock(block);
		}

		public static void WriteClass(this CodeUnit @this, string type, Action<CodeUnit> block)
		{
			@this.WriteChunkedLine("public sealed class ", type);
			@this.WriteBlock(block);
		}

		public static void WriteClass(this CodeUnit @this, string type, string @base, Action<CodeUnit> block)
		{
			@this.WriteChunkedLine("public sealed class ", type, " : ", @base);
			@this.WriteBlock(block);
		}

		public static void WriteProperty(this CodeUnit @this, string type, string name)
		{
			@this.WriteChunkedLine("public ", type, " ", name, " { get; set; }");
		}

		public static void WriteConstructor(this CodeUnit @this, string type, (string Type, string Name)[] parameters, Action<CodeUnit> block)
		{
			@this.WriteChunked("public ", type, "(");

			@this.WriteJoined(", ", parameters, parameter => @this.WriteChunked(parameter.Type, " ", parameter.Name));
			
			@this.WriteChunkedLine(")");
			@this.WriteBlock(block);
		}

		public static void WriteConstructor(this CodeUnit @this, string type, (string Type, string Name)[] parameters, string[] @base, Action<CodeUnit> block)
		{
			@this.WriteChunked("public ", type, "(");

			@this.WriteJoined(", ", parameters, parameter => @this.WriteChunked(parameter.Type, " ", parameter.Name));

			@this.WriteChunked(") : base(");

			@this.WriteJoined(", ", @base, forwardedParameter => @this.Code.Write(forwardedParameter));

			@this.WriteChunkedLine(")");
			@this.WriteBlock(block);
		}

		public static void WriteMethod(this CodeUnit @this, string type, string name, (string Type, string Name)[] parameters, Action<CodeUnit> block)
		{
			@this.WriteChunked("public ", type, " ", name, "(");

			@this.WriteJoined(", ", parameters, parameter => @this.WriteChunked(parameter.Type, " ", parameter.Name));

			@this.WriteChunkedLine(")");
			@this.WriteBlock(block);
		}

		public static void WriteChunked(this CodeUnit @this, params string[] chunks)
		{
			foreach (string chunk in chunks)
			{
				@this.Code.Write(chunk);
			}
		}

		public static void WriteChunkedLine(this CodeUnit @this, params string[] chunks)
		{
			@this.WriteChunked(chunks);
			@this.Code.WriteLine();
		}

		public static void WriteJoined<T>(this CodeUnit @this, string glue, T[] chunks, Action<T> chunkWriter)
		{
			if (0 < chunks.Length)
			{
				chunkWriter.Invoke(chunks[0]);

				for (int i = 1; i < chunks.Length; ++i)
				{
					@this.Code.Write(glue);
					chunkWriter.Invoke(chunks[i]);
				}
			}
		}

		public static void WriteBlock(this CodeUnit @this, Action<CodeUnit> block)
		{
			@this.Code.WriteLine("{");

			++@this.Code.Indent;
			{
				block.Invoke(@this);
			}
			--@this.Code.Indent;

			@this.Code.WriteLine("}");
		}
	}
}
