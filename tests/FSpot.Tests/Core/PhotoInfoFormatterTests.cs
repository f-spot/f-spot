// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;
using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public class PhotoInfoFormatterTests
	{
		[TestMethod]
		public void Name_UsesThePhotoNameWithoutMetadata ()
		{
			var photo = new FilePhoto (new SafeUri ("file:///tmp/vacation.jpg"));

			Assert.AreEqual ("vacation.jpg", PhotoInfoFormatter.Name (photo));
		}

		[TestMethod]
		public void Name_EmptyWhenThePhotoIsMissing ()
		{
			Assert.AreEqual (string.Empty, PhotoInfoFormatter.Name (null));
		}

		[TestMethod]
		public void Size_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Size (null));
		}

		[TestMethod]
		public void Exposure_NoneWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenNoneParen, PhotoInfoFormatter.Exposure (null));
		}

		[TestMethod]
		public void FocalLength_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.FocalLength (null));
		}

		[TestMethod]
		public void Camera_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Camera (null));
		}

		[TestMethod]
		public void Creator_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Creator (null));
		}

		[TestMethod]
		public void Date_UsesTheSharedShortCaptionPlusTime ()
		{
			var photo = new Moq.Mock<IPhoto> ();
			var time = new DateTime (2026, 6, 15, 14, 30, 0, DateTimeKind.Local);
			photo.Setup (p => p.Time).Returns (time);
			string date = PhotoInfoFormatter.Date (photo.Object);
			Assert.Contains (PhotoDateCaption.Format (photo.Object, wide: false), date);
			Assert.Contains (time.ToShortTimeString (), date);
		}

		[TestMethod]
		public void DateRange_SameDayUsesOnBetweenTimes ()
		{
			var morning = new Moq.Mock<IPhoto> ();
			var evening = new Moq.Mock<IPhoto> ();
			morning.Setup (p => p.Time).Returns (new DateTime (2026, 6, 15, 9, 0, 0, DateTimeKind.Local));
			evening.Setup (p => p.Time).Returns (new DateTime (2026, 6, 15, 18, 0, 0, DateTimeKind.Local));
			string range = PhotoInfoFormatter.DateRange (new[] { evening.Object, morning.Object });
			Assert.Contains (PhotoDateCaption.Format (morning.Object, wide: false), range);
			Assert.DoesNotContain ("{0}", range);
		}

		[TestMethod]
		public void DateRange_DifferentDaysUsesBetweenDates ()
		{
			var june = new Moq.Mock<IPhoto> ();
			var july = new Moq.Mock<IPhoto> ();
			june.Setup (p => p.Time).Returns (new DateTime (2026, 6, 15, 9, 0, 0, DateTimeKind.Local));
			july.Setup (p => p.Time).Returns (new DateTime (2026, 7, 2, 9, 0, 0, DateTimeKind.Local));
			string range = PhotoInfoFormatter.DateRange (new[] { july.Object, june.Object });
			Assert.Contains (PhotoDateCaption.Format (june.Object, wide: false), range);
			Assert.Contains (PhotoDateCaption.Format (july.Object, wide: false), range);
		}

		[TestMethod]
		public void Size_ReadsWidthAndHeightFromAJpeg ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			using (var metadata = FSpot.Utils.MetadataUtils.Parse (uri)) {
				Assert.IsNotNull (metadata);
				string size = PhotoInfoFormatter.Size (metadata);
				Assert.AreNotEqual (Strings.ParenUnknownParen, size);
				Assert.Contains ("x", size);
			}
		}
	}
}
