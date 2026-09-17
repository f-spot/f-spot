// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Slideshow;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class SlideshowSequenceTests
	{
		[TestMethod]
		public void VisitsTheCollectionInOrderAndWraps ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/c.jpg" }, startIndex: 0);

			Assert.AreEqual ("/a.jpg", sequence.Current);
			Assert.IsTrue (sequence.IsRunning);
			Assert.IsTrue (sequence.Advance ());
			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.IsTrue (sequence.Advance ());
			Assert.AreEqual ("/c.jpg", sequence.Current);
			Assert.IsTrue (sequence.Advance ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}

		[TestMethod]
		public void Stop_PreventsAdvance ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg" });
			sequence.Stop ();

			Assert.IsFalse (sequence.IsRunning);
			Assert.IsFalse (sequence.Advance ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}

		[TestMethod]
		public void StartsAtTheRequestedIndex ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/c.jpg" }, startIndex: 1);
			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.IsTrue (sequence.Retreat ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}
	}
}
