using System.IO;

namespace Nap
{
	public static class DirectoryInfoExtensions
	{
		public static DirectoryInfo Sub(this DirectoryInfo @this, string childPath) =>
			new DirectoryInfo(Path.Combine(@this.FullName, childPath));

		public static FileInfo File(this DirectoryInfo @this, string childPath) =>
			new FileInfo(Path.Combine(@this.FullName, childPath));

		public static FileInfo File(this DirectoryInfo @this, string childPath, string extension) =>
			new FileInfo(Path.Combine(@this.FullName, childPath + '.' + extension));
	}
}
