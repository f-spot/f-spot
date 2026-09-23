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

		static Photo PhotoAt (DateTime time)
			=> new Photo (null, null, 1, Hyena.DateTimeUtil.FromDateTime (time));
	}
}
