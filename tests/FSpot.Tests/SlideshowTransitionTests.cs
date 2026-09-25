// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Slideshow;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class SlideshowTransitionTests
	{
		[TestMethod]
		public void Timings_MatchTheGtkSlideShowWidget ()
		{
			Assert.AreEqual (TimeSpan.FromSeconds (6), SlideshowTransition.Interval);
			Assert.AreEqual (TimeSpan.FromSeconds (2), SlideshowTransition.Duration);
		}

		[TestMethod]
		public void All_ListsTheThreeGtkTransitions ()
		{
			CollectionAssert.AreEqual (
				new[] { SlideshowTransitionKind.Cover, SlideshowTransitionKind.Dissolve, SlideshowTransitionKind.Push },
				new System.Collections.Generic.List<SlideshowTransitionKind> (SlideshowTransition.All));
		}

		[TestMethod]
		public void Progress_ClampsToTheTransitionDuration ()
		{
			Assert.AreEqual (0, SlideshowTransition.Progress (TimeSpan.FromSeconds (-1)));
			Assert.AreEqual (0.5, SlideshowTransition.Progress (TimeSpan.FromSeconds (1)), 1e-9);
			Assert.AreEqual (1, SlideshowTransition.Progress (TimeSpan.FromSeconds (5)));
		}

		[TestMethod]
		public void Push_SlidesBothPhotosLeft ()
		{
			var frame = SlideshowTransition.Frame (SlideshowTransitionKind.Push, 0.25);

			Assert.AreEqual (-0.25, frame.PreviousOffset, 1e-9);
			Assert.AreEqual (0.75, frame.NextOffset, 1e-9);
			Assert.AreEqual (1, frame.PreviousOpacity);
			Assert.AreEqual (1, frame.NextOpacity);
		}

		[TestMethod]
		public void Cover_SlidesTheNewPhotoOverAStillOldOne ()
		{
			var frame = SlideshowTransition.Frame (SlideshowTransitionKind.Cover, 0.25);

			Assert.AreEqual (0, frame.PreviousOffset);
			Assert.AreEqual (0.75, frame.NextOffset, 1e-9);
			Assert.AreEqual (1, frame.NextOpacity);
		}

		[TestMethod]
		public void Dissolve_CrossFadesInPlace ()
		{
			var frame = SlideshowTransition.Frame (SlideshowTransitionKind.Dissolve, 0.25);

			Assert.AreEqual (0, frame.PreviousOffset);
			Assert.AreEqual (0, frame.NextOffset);
			Assert.AreEqual (1, frame.PreviousOpacity);
			Assert.AreEqual (0.25, frame.NextOpacity, 1e-9);
		}

		[TestMethod]
		public void EveryTransition_EndsWithTheNewPhotoFullyInPlace ()
		{
			foreach (var kind in SlideshowTransition.All) {
				var frame = SlideshowTransition.Frame (kind, 1.5);
				Assert.AreEqual (0, frame.NextOffset, 1e-9, kind.ToString ());
				Assert.AreEqual (1, frame.NextOpacity, 1e-9, kind.ToString ());
			}
		}

		[TestMethod]
		public void EveryTransition_StartsWithTheOldPhotoShowing ()
		{
			foreach (var kind in SlideshowTransition.All) {
				var frame = SlideshowTransition.Frame (kind, -1);
				Assert.AreEqual (0, frame.PreviousOffset, 1e-9, kind.ToString ());
				Assert.AreEqual (1, frame.PreviousOpacity, 1e-9, kind.ToString ());
				bool nextHidden = frame.NextOpacity == 0 || Math.Abs (frame.NextOffset) >= 1;
				Assert.IsTrue (nextHidden, kind.ToString ());
			}
		}
	}
}
