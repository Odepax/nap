using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace Nap {
	public sealed class TextFileBuilder {
		readonly string Name;
		public readonly CodeBuilder Code = new();

		/// <param name="name"> Just the name, not the full path. </param>
		public TextFileBuilder(string name) => Name = name;

		/// <summary>
		/// For instance:
		///
		/// <code>
		/// new TextFileBuilder("note.txt").ProvisionAsAsync(".../as.txt");
		/// </code>
		///
		/// Will generate the following tree:
		///
		/// <code>
		/// .../
		///    as.txt
		/// </code>
		/// </summary>
		public Task ProvisionAsAsync(string outputPath) {
			IODirectory.CreateDirectory(IODirectory.GetParent(outputPath)?.FullName ?? outputPath);
			return IOFile.WriteAllTextAsync(outputPath, Code.ToString(), Encoding.UTF8);
		}

		/// <summary>
		/// For instance:
		///
		/// <code>
		/// new TextFileBuilder("note.txt").ProvisionInAsync(".../in");
		/// </code>
		///
		/// Will generate the following tree:
		///
		/// <code>
		/// .../
		///    in/
		///       note.txt
		/// </code>
		/// </summary>
		public Task ProvisionInAsync(string outputPath) {
			IODirectory.CreateDirectory(outputPath);
			return IOFile.WriteAllTextAsync(IOPath.Join(outputPath, Name), Code.ToString(), Encoding.UTF8);
		}
	}

	public sealed class DirectoryBuilder {
		readonly struct File {
			public readonly string Path;
			public readonly CodeBuilder Code;

			public File(string path, CodeBuilder code) {
				Path = path;
				Code = code;
			}
		}

		readonly string Name;
		readonly string Path;
		readonly List<string> Directories;
		readonly List<File> Files;

		/// <param name="name"> Just the name, not the full path. </param>
		public DirectoryBuilder(string name) : this(name, string.Empty, new(), new()) {}

		DirectoryBuilder(string name, string path, List<string> directories, List<File> files) {
			Name = name;
			Path = path;
			Directories = directories;
			Files = files;
		}

		/// <param name="name"> Just the name, not the full path. </param>
		public DirectoryBuilder Directory(string name) {
			var path = IOPath.Join(Path, name);
			var directory = new DirectoryBuilder(name, path, Directories, Files);

			Directories.Add(path);

			return directory;
		}

		/// <param name="name"> Just the name, not the full path. </param>
		public TextFileBuilder TextFile(string name) {
			var path = IOPath.Join(Path, name);
			var file = new TextFileBuilder(name);

			Files.Add(new File(path, file.Code));

			return file;
		}

		/// <summary>
		/// For instance:
		///
		/// <code>
		/// var root = new DirectoryBuilder("root");
		/// var sub = root.Directory("sub");
		/// 
		/// await root.ProvisionAsAsync(".../as");
		/// </code>
		///
		/// Will generate the following tree:
		///
		/// <code>
		/// .../
		///    as/
		///       sub/
		/// </code>
		/// </summary>
		public Task ProvisionAsAsync(string outputPath) {
			IODirectory.CreateDirectory(outputPath);

			foreach (var path in Directories)
				IODirectory.CreateDirectory(IOPath.Join(outputPath, path));

			return Task.WhenAll(
				Files.Select(file =>
					IOFile.WriteAllTextAsync(IOPath.Join(outputPath, file.Path), file.Code.ToString(), Encoding.UTF8)
				)
			);
		}

		/// <summary>
		/// For instance:
		///
		/// <code>
		/// var root = new DirectoryBuilder("root");
		/// var sub = root.Directory("sub");
		/// 
		/// await root.ProvisionInAsync(".../in");
		/// </code>
		///
		/// Will generate the following tree:
		///
		/// <code>
		/// .../
		///    in/
		///       root/
		///          sub/
		/// </code>
		/// </summary>
		public Task ProvisionInAsync(string outputPath) =>
			ProvisionAsAsync(IOPath.Join(outputPath, Name));
	}
}
