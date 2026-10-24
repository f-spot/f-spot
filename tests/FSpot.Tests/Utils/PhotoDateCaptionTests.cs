// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

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
		public void Format_NamesYearThenMonthThenDay ()
		{
			var photo = new Mock<IPhoto> ();
			var time = new DateTime (2026, 1, 2, 15, 4, 5, DateTimeKind.Unspecified);
			photo.Setup (p => p.Time).Returns (time);

			Assert.AreEqual ("2026-01-02", PhotoDateCaption.Format (photo.Object, wide: false));
			Assert.AreEqual ("2026-01-02 15:04:05", PhotoDateCaption.Format (photo.Object, wide: true));
			Assert.AreEqual ("15:04:05", PhotoDateCaption.FormatTime (time));
		}

		[TestMethod]
		public void Format_DoesNotFollowTheCurrentCulture ()
		{
			var time = new DateTime (2026, 1, 2, 15, 4, 5, DateTimeKind.Unspecified);
			CultureInfo saved = CultureInfo.CurrentCulture;
			try {
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo ("en-US");
				string american = PhotoDateCaption.Format (time, wide: false);
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo ("fr-FR");
				string french = PhotoDateCaption.Format (time, wide: false);
				Assert.AreEqual ("2026-01-02", american);
				Assert.AreEqual (american, french);
			} finally {
				CultureInfo.CurrentCulture = saved;
			}
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
			DateTime local = utc.ToLocalTime ();
			Assert.AreEqual (local.ToString ("yyyy-MM-dd", CultureInfo.InvariantCulture), PhotoDateCaption.FormatLocal (photo.Object, wide: false));
			Assert.AreEqual (local.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), PhotoDateCaption.FormatLocal (photo.Object, wide: true));
		}
	}
}
