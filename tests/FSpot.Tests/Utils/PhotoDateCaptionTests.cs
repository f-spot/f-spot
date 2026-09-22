// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Tests.Utils
{
	[TestClass]
	public sealed class PhotoDateCaptionTests
	{
		[TestMethod]
		public void Format_ShortDateWhenNarrow_FullWhenWide ()
		{
			var photo = new Mock<IPhoto> ();
			var time = new DateTime (2026, 6, 15, 14, 30, 0, DateTimeKind.Local);
			photo.Setup (p => p.Time).Returns (time);

			Assert.AreEqual (time.ToShortDateString (), PhotoDateCaption.Format (photo.Object, wide: false));
			Assert.AreEqual (time.ToString (), PhotoDateCaption.Format (photo.Object, wide: true));
		}

		[TestMethod]
		public void Format_EmptyForInvalidPhotos ()
		{
			var photo = new Mock<IPhoto> ();
			photo.As<IInvalidPhotoCheck> ().Setup (p => p.IsInvalid).Returns (true);
			photo.Setup (p => p.Time).Returns (DateTime.UtcNow);
			Assert.AreEqual (string.Empty, PhotoDateCaption.Format (photo.Object, wide: false));
		}

		[TestMethod]
		public void FormatLocal_UsesTheLocalShortDate ()
		{
			var photo = new Mock<IPhoto> ();
			var utc = new DateTime (2026, 6, 15, 23, 30, 0, DateTimeKind.Utc);
			photo.Setup (p => p.Time).Returns (utc);
			Assert.AreEqual (utc.ToLocalTime ().ToShortDateString (), PhotoDateCaption.FormatLocal (photo.Object, wide: false));
			Assert.AreEqual (utc.ToLocalTime ().ToString (), PhotoDateCaption.FormatLocal (photo.Object, wide: true));
		}
	}
}
