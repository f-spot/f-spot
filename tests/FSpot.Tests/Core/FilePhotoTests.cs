// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public class FilePhotoTests
	{
		[TestMethod]
		public void IsInvalid_FalseForAReadableJpeg ()
		{
			var photo = new FilePhoto (ImageTestHelper.CreateTempFile ("taglib-sample.jpg"));

			Assert.IsFalse (photo.IsInvalid);
		}

		[TestMethod]
		public void IsInvalid_TrueWhenTheFileIsMissing ()
		{
			var photo = new FilePhoto (new SafeUri (Path.Combine (Path.GetTempPath (), "f-spot-missing-" + Path.GetRandomFileName () + ".jpg")));

			Assert.IsTrue (photo.IsInvalid);
		}

		[TestMethod]
		public void IsInvalid_TrueForABrokenJpeg ()
		{
			var photo = new FilePhoto (ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg"));

			Assert.IsTrue (photo.IsInvalid);
		}

		[TestMethod]
		public void Time_DoesNotThrowWhenTheFileIsCorrupt ()
		{
			var photo = new FilePhoto (ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg"));

			DateTime time = photo.Time;

			Assert.IsTrue (photo.IsInvalid);
			Assert.AreNotEqual (default (DateTime), time);
		}

		[TestMethod]
		public void Description_DoesNotThrowWhenTheFileIsCorrupt ()
		{
			var photo = new FilePhoto (ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg"));

			Assert.IsNull (photo.Description);
			Assert.IsTrue (photo.IsInvalid);
		}

		[TestMethod]
		public void Time_DoesNotThrowWhenTheFileIsMissing ()
		{
			var photo = new FilePhoto (new SafeUri (Path.Combine (Path.GetTempPath (), "f-spot-missing-" + Path.GetRandomFileName () + ".jpg")));

			_ = photo.Time;

			Assert.IsTrue (photo.IsInvalid);
		}

		[TestMethod]
		public void Description_DoesNotThrowWhenTheFileIsMissing ()
		{
			var photo = new FilePhoto (new SafeUri (Path.Combine (Path.GetTempPath (), "f-spot-missing-" + Path.GetRandomFileName () + ".jpg")));

			Assert.IsNull (photo.Description);
			Assert.IsTrue (photo.IsInvalid);
		}

		[TestMethod]
		public void Time_UsesEmbeddedDateWhenTheJpegIsReadable ()
		{
			var photo = new FilePhoto (ImageTestHelper.CreateTempFile ("taglib-sample.jpg"));

			Assert.IsFalse (photo.IsInvalid);
			Assert.AreNotEqual (default (DateTime), photo.Time);
			Assert.AreNotEqual (DateTime.MinValue, photo.Time);
		}

		[TestMethod]
		[DataRow (".svg")]
		[DataRow (".svgz")]
		public void IsInvalid_FalseForAnSvgFile (string extension)
		{
			string path = Path.Combine (Path.GetTempPath (), "f-spot-svg-" + Path.GetRandomFileName () + extension);
			File.WriteAllText (path, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"8\" height=\"8\"><rect width=\"8\" height=\"8\" fill=\"#00ff00\"/></svg>");
			try {
				var photo = new FilePhoto (new SafeUri (path));

				Assert.IsFalse (photo.IsInvalid);
				_ = photo.Time;
			} finally {
				File.Delete (path);
			}
		}
	}
}
