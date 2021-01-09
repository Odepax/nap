using NUnit.Framework;

namespace Nap.SourceGenerators.Tests {
	// An immutable sample class.
	[GenerateImmutable]
	public partial class Bird {
		public int Id { get; set; }
	}

	// A readonly sample class.
	[GenerateReadOnly]
	public partial class Cat {
		public int Id { get; set; }
	}

	// An immutable and readonly sample class.
	[GenerateImmutable]
	[GenerateReadOnly]
	public partial class Dog {
		public int Id { get; set; }
	}

	static class ImmutableAndReadOnlyGeneratorConsumerTests {
		[Test]
		public static void As_readonly_returns_the_same_object() {
			var cat = new Cat { Id = 1 };
			var dog = new Dog { Id = 1 };

			var ro_cat = cat.AsReadOnly();
			var ro_dog = dog.AsReadOnly();

			Assert.AreSame(cat, ro_cat);
			Assert.AreSame(dog, ro_dog);
		}

		[Test]
		public static void To_immutable_returns_a_new_object() {
			var bird = new Bird { Id = 1 };
			var dog = new Dog { Id = 1 };

			var im_bird = bird.ToImmutable();
			var im_dog = dog.ToImmutable();

			Assert.AreNotSame(bird, im_bird);
			Assert.AreNotSame(dog, im_dog);
		}

		[Test]
		public static void To_mutable_returns_a_new_object() {
			var cat = new Cat { Id = 1 };
			var dog = new Dog { Id = 1 };

			var mu_cat = cat.ToMutable();
			var mu_dog = dog.ToMutable();

			Assert.AreNotSame(cat, mu_cat);
			Assert.AreNotSame(dog, mu_dog);
		}

		/*



		*/
	}
}
