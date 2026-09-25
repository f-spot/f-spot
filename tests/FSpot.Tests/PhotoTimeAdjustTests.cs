// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoTimeAdjustTests
	{
		[TestMethod]
		public void ShiftEach_MovesEveryPhotoByTheSameOffset ()
		{
			var first = PhotoAt (new DateTime (2020, 1, 1, 12, 0, 0));
			var second = PhotoAt (new DateTime (2020, 6, 1, 12, 0, 0));
			Photo[] committed = null;

			PhotoTimeAdjust.ShiftEach (new[] { first, second }, TimeSpan.FromHours (2), photos => committed = photos);

			Assert.AreEqual (new DateTime (2020, 1, 1, 14, 0, 0), first.Time);
			Assert.AreEqual (new DateTime (2020, 6, 1, 14, 0, 0), second.Time);
			Assert.HasCount (2, committed);
		}

		[TestMethod]
		public void Space_StepsOutFromTheAnchor ()
		{
			var photos = new[] {
				PhotoAt (new DateTime (2020, 1, 1)),
				PhotoAt (new DateTime (2020, 2, 1)),
				PhotoAt (new DateTime (2020, 3, 1))
			};
			var anchor = new DateTime (2020, 5, 1, 8, 0, 0);

			PhotoTimeAdjust.Space (photos, 1, anchor, TimeSpan.FromMinutes (10), null);

			Assert.AreEqual (anchor - TimeSpan.FromMinutes (10), photos[0].Time);
			Assert.AreEqual (anchor, photos[1].Time);
			Assert.AreEqual (anchor + TimeSpan.FromMinutes (10), photos[2].Time);
		}

		[TestMethod]
		public void PreviewTime_WhenSpacing_MatchesWhatSpaceWouldAssign ()
		{
			var originals = new[] {
				new DateTime (2020, 1, 1),
				new DateTime (2020, 2, 1),
				new DateTime (2020, 3, 1)
			};
			var anchorNewTime = new DateTime (2020, 5, 1, 8, 0, 0);
			var step = TimeSpan.FromMinutes (10);
			const int anchorIndex = 1;

			var photos = new[] { PhotoAt (originals[0]), PhotoAt (originals[1]), PhotoAt (originals[2]) };
			PhotoTimeAdjust.Space (photos, anchorIndex, anchorNewTime, step, null);

			for (int i = 0; i < originals.Length; i++)
				Assert.AreEqual (photos[i].Time, PhotoTimeAdjust.PreviewTime (originals, i, anchorIndex, anchorNewTime, shiftAll: false, step));
		}

		[TestMethod]
		public void PreviewTime_WhenSpacing_AnchorsOnTheSelectedRowNotIndexZero ()
		{
			var originals = new[] { new DateTime (2020, 1, 1), new DateTime (2020, 1, 2), new DateTime (2020, 1, 3) };
			var anchorNewTime = new DateTime (2020, 6, 1, 12, 0, 0);
			var step = TimeSpan.FromMinutes (5);

			// Anchored on row 2 (not row 0): row 2 keeps the adjusted time exactly.
			Assert.AreEqual (anchorNewTime, PhotoTimeAdjust.PreviewTime (originals, 2, 2, anchorNewTime, shiftAll: false, step));
			// Row 0, two steps before the anchor, lands ten minutes earlier.
			Assert.AreEqual (anchorNewTime - TimeSpan.FromMinutes (10), PhotoTimeAdjust.PreviewTime (originals, 0, 2, anchorNewTime, shiftAll: false, step));
		}

		[TestMethod]
		public void PreviewTime_WhenShifting_MovesEveryPhotoByTheAnchorsOffset ()
		{
			var originals = new[] { new DateTime (2020, 1, 1, 9, 0, 0), new DateTime (2020, 1, 2, 9, 0, 0) };
			var anchorNewTime = originals[0] + TimeSpan.FromHours (2);

			Assert.AreEqual (originals[0] + TimeSpan.FromHours (2), PhotoTimeAdjust.PreviewTime (originals, 0, 0, anchorNewTime, shiftAll: true, TimeSpan.Zero));
			Assert.AreEqual (originals[1] + TimeSpan.FromHours (2), PhotoTimeAdjust.PreviewTime (originals, 1, 0, anchorNewTime, shiftAll: true, TimeSpan.Zero));
		}

		static Photo PhotoAt (DateTime time)
			=> new Photo (null, null, 1, Hyena.DateTimeUtil.FromDateTime (time));
	}
}
