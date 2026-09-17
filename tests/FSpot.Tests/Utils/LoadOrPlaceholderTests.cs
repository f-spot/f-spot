// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	// Draw paths (Filmstrip, TryLoadIcon, PixbufCache) must never throw when a
	// theme icon or thumbnail is missing, and must never hand a null image to
	// expose. LoadOrPlaceholder is that policy: try each loader, skip nulls and
	// throws, then a sized placeholder.
	[TestClass]
	public class LoadOrPlaceholderTests
	{
		static string Placeholder (int size) => $"placeholder-{size}";

		[TestMethod]
		public void Resolve_ReturnsTheFirstSuccessfulLoad ()
		{
			string result = LoadOrPlaceholder.Resolve (64, Placeholder,
				() => "icon",
				() => "stock");

			Assert.AreEqual ("icon", result);
		}

		[TestMethod]
		public void Resolve_SkipsANullLoadAndUsesTheNext ()
		{
			string result = LoadOrPlaceholder.Resolve (64, Placeholder,
				() => null,
				() => "stock");

			Assert.AreEqual ("stock", result);
		}

		[TestMethod]
		public void Resolve_SkipsAThrowingLoadAndUsesTheNext ()
		{
			string result = LoadOrPlaceholder.Resolve (64, Placeholder,
				() => throw new InvalidOperationException ("Icon 'gtk-missing-image' not present in theme"),
				() => "stock");

			Assert.AreEqual ("stock", result);
		}

		[TestMethod]
		public void Resolve_UsesThePlaceholderWhenEveryLoadFails ()
		{
			string result = LoadOrPlaceholder.Resolve (64, Placeholder,
				() => throw new InvalidOperationException ("unrecognized image file format"),
				() => null);

			Assert.AreEqual ("placeholder-64", result);
		}

		[TestMethod]
		public void Resolve_DoesNotCallLaterAttemptsAfterASuccess ()
		{
			bool secondCalled = false;

			string result = LoadOrPlaceholder.Resolve (64, Placeholder,
				() => "icon",
				() => {
					secondCalled = true;
					return "stock";
				});

			Assert.AreEqual ("icon", result);
			Assert.IsFalse (secondCalled);
		}

		[TestMethod]
		public void Resolve_DoesNotBuildAPlaceholderWhenALoadSucceeds ()
		{
			bool placeholderCalled = false;

			string result = LoadOrPlaceholder.Resolve (64,
				size => {
					placeholderCalled = true;
					return Placeholder (size);
				},
				() => "icon");

			Assert.AreEqual ("icon", result);
			Assert.IsFalse (placeholderCalled);
		}

		[TestMethod]
		[DataRow (0)]
		[DataRow (-8)]
		public void Resolve_AsksThePlaceholderForAtLeastOnePixel (int size)
		{
			string result = LoadOrPlaceholder.Resolve (size, Placeholder, () => null);

			Assert.AreEqual ("placeholder-1", result);
		}

		[TestMethod]
		public void Resolve_UsesThePlaceholderWhenThereAreNoAttempts ()
		{
			string result = LoadOrPlaceholder.Resolve (48, Placeholder);

			Assert.AreEqual ("placeholder-48", result);
		}

		[TestMethod]
		public void Resolve_SkipsANullAttemptAndUsesTheNext ()
		{
			string result = LoadOrPlaceholder.Resolve (64, Placeholder, null, () => "icon");

			Assert.AreEqual ("icon", result);
		}
	}
}
