using System.Collections.Generic;

namespace Nap.Cli.Definitions
{
	public sealed class Resource
	{
		public string Name { get; }
		public IReadOnlyDictionary<string, string> Fields { get; }

		public Resource(string name, IReadOnlyDictionary<string, string> fields)
		{
			Name = name;
			Fields = fields;
		}
	}
}
