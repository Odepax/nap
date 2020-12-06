using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nap.CsGeneration
{
	public readonly struct CodeUnit : IDisposable
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CodeUnit New() =>
			new CodeUnit(new SortedSet<string>());

		private readonly StringWriter CodeText;

		public readonly ISet<string> Usings;
		public readonly IndentedTextWriter Code;

		private CodeUnit(ISet<string> usings)
		{
			CodeText = new StringWriter();
			Usings = usings;
			Code = new IndentedTextWriter(CodeText, "\t");
		}

		public override string ToString()
		{
			StringBuilder code = new StringBuilder();

			if (0 < Usings.Count)
			{
				foreach (string @using in Usings)
				{
					code.Append("using ").Append(@using).AppendLine(";");
				}

				code.AppendLine(); 
			}

			code.Append(CodeText.ToString());
			
			return code.ToString();
		}

		public void Dispose()
		{
			CodeText?.Dispose();
			Code?.Dispose();
		}
	}
}
