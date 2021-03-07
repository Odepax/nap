using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Nap.Tests {
	static class FsBuilderTests {
		static string Tmp = string.Empty;

		[SetUp]
		public static void SetUp() {
			Tmp = Path.Join(Path.GetTempPath(), "Nap.Tests", Path.GetRandomFileName());
		}

		[OneTimeTearDown]
		public static void OneTimeTearDown() {
			Directory.Delete(Path.Join(Path.GetTempPath(), "Nap.Tests"), recursive: true);
		}

		// Files
		// ----

		[Test]
		public static async Task Provision_empty_file_as() {
			await new TextFileBuilder("hola.txt").ProvisionAsAsync(Path.Join(Tmp, "hello.txt"));

			AssertFile("hello.txt", string.Empty);
			AssertNoFile("hola.txt");
		}

		[Test]
		public static async Task Provision_empty_file_in() {
			await new TextFileBuilder("hola.txt").ProvisionInAsync(Path.Join(Tmp, "hello"));

			AssertFile(Path.Join("hello", "hola.txt"), string.Empty);
		}

		[Test]
		public static async Task Provision_text_file_as() {
			var file = new TextFileBuilder("hola.txt");

			file.Code.Append("Hello, there!");

			await file.ProvisionAsAsync(Path.Join(Tmp, "hello.txt"));

			AssertFile("hello.txt", "Hello, there!");
			AssertNoFile("hola.txt");
		}

		[Test]
		public static async Task Provision_text_file_in() {
			var file = new TextFileBuilder("hola.txt");

			file.Code.Append("Hello, there!");

			await file.ProvisionInAsync(Path.Join(Tmp, "hello"));

			AssertFile(Path.Join("hello", "hola.txt"), "Hello, there!");
		}

		// Directories
		// ----

		[Test]
		public static void Provision_empty_dir_as() {
			new DirectoryBuilder("src").ProvisionAsAsync(Path.Join(Tmp, "hello")).GetAwaiter().GetResult();

			AssertDirectory("hello", 0);
			AssertNoDirectory("src");
		}

		[Test]
		public static async Task Provision_empty_dir_in() {
			await new DirectoryBuilder("src").ProvisionInAsync(Path.Join(Tmp, "hello"));

			AssertDirectory(Path.Join("hello", "src"), 0);
		}

		[Test]
		public static async Task Provision_simple_dir_as() {
			var directory = new DirectoryBuilder("src");
			
			directory.TextFile("config.ini").Code.Append("SOME_CONFIG = 'SOME VALUE'");
			directory.TextFile("index.js").Code.Append("require('config').getFromFile('./config.txt').log('SOME_CONFIG')");

			await directory.ProvisionAsAsync(Tmp);

			AssertDirectory("", 2);

			AssertFile("config.ini", "SOME_CONFIG = 'SOME VALUE'");
			AssertFile("index.js", "require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
		}

		[Test]
		public static async Task Provision_simple_dir_in() {
			var directory = new DirectoryBuilder("src");

			directory.TextFile("config.ini").Code.Append("SOME_CONFIG = 'SOME VALUE'");
			directory.TextFile("index.js").Code.Append("require('config').getFromFile('./config.txt').log('SOME_CONFIG')");

			await directory.ProvisionInAsync(Tmp);

			AssertDirectory("", 1);
			AssertDirectory("src", 2);

			AssertFile(Path.Join("src", "config.ini"), "SOME_CONFIG = 'SOME VALUE'");
			AssertFile(Path.Join("src", "index.js"), "require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
		}

		[Test]
		public static async Task Provision_recursive_dir_as() {
			var directory = new DirectoryBuilder("root");

			directory.Directory("out");
			var cfg = directory.Directory("cfg");
			var src = directory.Directory("src");
			var lib = src.Directory("lib").Directory("config");

			directory.TextFile("package.json").Code.Append("{ \"private\": true }");
			cfg.TextFile("config.ini").Code.Append("SOME_CONFIG = 'SOME VALUE'");
			src.TextFile("index.js").Code.Append("require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
			lib.TextFile("config.js").Code.Append("// Test.");

			await directory.ProvisionAsAsync(Tmp);

			AssertDirectory("", 4);
			AssertDirectory("cfg", 1);
			AssertDirectory("src", 2);
			AssertDirectory(Path.Join("src", "lib"), 1);
			AssertDirectory(Path.Join("src", "lib", "config"), 1);
			AssertDirectory(Path.Join("out"), 0);

			AssertFile("package.json", "{ \"private\": true }");
			AssertFile(Path.Join("cfg", "config.ini"), "SOME_CONFIG = 'SOME VALUE'");
			AssertFile(Path.Join("src", "index.js"), "require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
			AssertFile(Path.Join("src", "lib", "config", "config.js"), "// Test.");
		}

		[Test]
		public static async Task Provision_recursive_dir_in() {
			var directory = new DirectoryBuilder("root");

			directory.Directory("out");
			var cfg = directory.Directory("cfg");
			var src = directory.Directory("src");
			var lib = src.Directory("lib").Directory("config");

			directory.TextFile("package.json").Code.Append("{ \"private\": true }");
			cfg.TextFile("config.ini").Code.Append("SOME_CONFIG = 'SOME VALUE'");
			src.TextFile("index.js").Code.Append("require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
			lib.TextFile("config.js").Code.Append("// Test.");

			await directory.ProvisionInAsync(Tmp);

			AssertDirectory("root", 4);
			AssertDirectory(Path.Join("root", "cfg"), 1);
			AssertDirectory(Path.Join("root", "src"), 2);
			AssertDirectory(Path.Join("root", "src", "lib"), 1);
			AssertDirectory(Path.Join("root", "src", "lib", "config"), 1);
			AssertDirectory(Path.Join("root", "out"), 0);

			AssertFile(Path.Join("root", "package.json"), "{ \"private\": true }");
			AssertFile(Path.Join("root", "cfg", "config.ini"), "SOME_CONFIG = 'SOME VALUE'");
			AssertFile(Path.Join("root", "src", "index.js"), "require('config').getFromFile('./config.txt').log('SOME_CONFIG')");
			AssertFile(Path.Join("root", "src", "lib", "config", "config.js"), "// Test.");
		}

		// Assertions
		// ----

		static void AssertFile(string path, string expectedContent) {
			path = Path.Join(Tmp, path);

			FileAssert.Exists(path);

			var actualContent = File.ReadAllText(path, Encoding.UTF8);

			Assert.AreEqual(expectedContent, actualContent);
		}

		static void AssertNoFile(string path) {
			path = Path.Join(Tmp, path);

			FileAssert.DoesNotExist(path);
		}

		static void AssertDirectory(string path, int expectedChildCount) {
			path = Path.Join(Tmp, path);

			DirectoryAssert.Exists(path);

			var actualChildCount = Directory.GetFileSystemEntries(path).Length;

			Assert.AreEqual(expectedChildCount, actualChildCount);
		}

		static void AssertNoDirectory(string path) {
			path = Path.Join(Tmp, path);

			DirectoryAssert.DoesNotExist(path);
		}
	}
}
