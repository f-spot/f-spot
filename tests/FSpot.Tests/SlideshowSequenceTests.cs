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
		public void Replace_PointsTheCurrentSlideAtTheSurvivingFile ()
		{
			var sequence = new SlideshowSequence (new[] { "/edit.jpg", "/b.jpg" }, startIndex: 0);

			Assert.IsTrue (sequence.Replace ("/edit.jpg", "/original.jpg"));

			Assert.AreEqual ("/original.jpg", sequence.Current);
			Assert.AreEqual (2, sequence.Count);
			Assert.IsTrue (sequence.Advance ());
			Assert.AreEqual ("/b.jpg", sequence.Current);
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

		[TestMethod]
		public void MoveTo_ClampsAndReportsChange ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/c.jpg" });

			Assert.IsTrue (sequence.MoveTo (9));
			Assert.AreEqual ("/c.jpg", sequence.Current);
			Assert.IsFalse (sequence.MoveTo (2));
		}

		[TestMethod]
		public void Remove_DropsTheNamedPhotoAfterTheShowHasMovedOn ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/c.jpg" }, startIndex: 0);
			Assert.IsTrue (sequence.Advance ());

			Assert.IsTrue (sequence.Remove ("/a.jpg"));
			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.AreEqual (2, sequence.Count);
			Assert.IsFalse (sequence.Remove ("/missing.jpg"));
		}

		[TestMethod]
		public void RemoveCurrent_ShowsTheFollowingPhoto ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/c.jpg" }, startIndex: 1);

			Assert.IsTrue (sequence.RemoveCurrent ());
			Assert.AreEqual ("/c.jpg", sequence.Current);
			Assert.AreEqual (2, sequence.Count);
		}

		[TestMethod]
		public void RemoveCurrent_AtTheEnd_StepsBack ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg" }, startIndex: 1);

			Assert.IsTrue (sequence.RemoveCurrent ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}

		[TestMethod]
		public void RemoveCurrent_LastPhoto_StopsTheSequence ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg" });

			Assert.IsFalse (sequence.RemoveCurrent ());
			Assert.AreEqual (0, sequence.Count);
			Assert.IsFalse (sequence.IsRunning);
			Assert.AreEqual (string.Empty, sequence.Current);
		}

		[TestMethod]
		public void Remove_ShownDuplicate_DropsThatCopyAndShowsTheFollower ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/a.jpg", "/b.jpg" }, startIndex: 1);

			Assert.IsTrue (sequence.Remove ("/a.jpg"));

			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.AreEqual (1, sequence.Index);
			Assert.IsTrue (sequence.Retreat ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}

		[TestMethod]
		public void RemoveCurrent_DuplicateAtTheEnd_StepsBackToThePreviousPhoto ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/a.jpg" }, startIndex: 2);

			Assert.IsTrue (sequence.RemoveCurrent ());

			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.AreEqual (1, sequence.Index);
		}

		[TestMethod]
		public void Remove_DuplicateThatIsNotCurrent_DropsTheFirstCopy ()
		{
			var sequence = new SlideshowSequence (new[] { "/a.jpg", "/b.jpg", "/a.jpg" }, startIndex: 1);

			Assert.IsTrue (sequence.Remove ("/a.jpg"));

			Assert.AreEqual ("/b.jpg", sequence.Current);
			Assert.AreEqual (0, sequence.Index);
			Assert.IsTrue (sequence.Advance ());
			Assert.AreEqual ("/a.jpg", sequence.Current);
		}

		[TestMethod]
		public void Constructor_CopiesThePaths ()
		{
			var paths = new System.Collections.Generic.List<string> { "/a.jpg", "/b.jpg" };
			var sequence = new SlideshowSequence (paths);

			sequence.RemoveCurrent ();

			Assert.HasCount (2, paths);
		}
	}
}
