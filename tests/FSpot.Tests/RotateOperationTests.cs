// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Thumbnail;
using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib.Image;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class RotateOperationTests
	{
		[TestMethod]
		public void Step_Clockwise_WritesRotatedOrientationAndDeletesThumbnailsOnce ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			try {
				var before = OrientationOf (uri);
				var thumbs = new Mock<IThumbnailService> ();
				var op = new RotateOperation (new FilePhoto (uri), RotateDirection.Clockwise, thumbs.Object);

				// Step returns true while the current photo still has work; a
				// single EXIF write finishes the item, so the first call is false.
				Assert.IsFalse (op.Step ());
				Assert.IsFalse (op.Step ());

				Assert.AreEqual (ImageOrientations.Rotate90 (before), OrientationOf (uri));
				thumbs.Verify (t => t.DeleteThumbnails (It.Is<SafeUri> (u => u.AbsoluteUri == uri.AbsoluteUri)), Times.Once);
			} finally {
				ImageTestHelper.DeleteTempFile (uri);
			}
		}

		[TestMethod]
		public void Step_Counterclockwise_WritesRotate270 ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			try {
				var before = OrientationOf (uri);
				var op = new RotateOperation (new FilePhoto (uri), RotateDirection.Counterclockwise,
					Mock.Of<IThumbnailService> ());

				op.Step ();

				Assert.AreEqual (ImageOrientations.Rotate270 (before), OrientationOf (uri));
			} finally {
				ImageTestHelper.DeleteTempFile (uri);
			}
		}

		[TestMethod]
		public void Step_ReadOnlyFile_ThrowsRotateException ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			try {
				System.IO.File.SetAttributes (uri.AbsolutePath, FileAttributes.ReadOnly);
				var op = new RotateOperation (new FilePhoto (uri), RotateDirection.Clockwise,
					Mock.Of<IThumbnailService> ());

				var ex = Assert.ThrowsExactly<RotateException> (() => op.Step ());
				Assert.IsTrue (ex.ReadOnly);
				Assert.AreEqual (uri.LocalPath, ex.Path);
			} finally {
				System.IO.File.SetAttributes (uri.AbsolutePath, FileAttributes.Normal);
				ImageTestHelper.DeleteTempFile (uri);
			}
		}

		[TestMethod]
		public void RotateMultiple_StepsOncePerPhotoThenStops ()
		{
			var first = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var second = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			try {
				var beforeFirst = OrientationOf (first);
				var beforeSecond = OrientationOf (second);
				var items = new IPhoto[] { new FilePhoto (first), new FilePhoto (second) };
				var op = new RotateMultiple (items, RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

				Assert.IsTrue (op.Step ());
				Assert.AreEqual (1, op.Index);
				Assert.IsFalse (op.Step ());
				Assert.AreEqual (2, op.Index);
				Assert.IsFalse (op.Step ());
				Assert.AreEqual (ImageOrientations.Rotate90 (beforeFirst), OrientationOf (first));
				Assert.AreEqual (ImageOrientations.Rotate90 (beforeSecond), OrientationOf (second));
			} finally {
				ImageTestHelper.DeleteTempFile (first);
				ImageTestHelper.DeleteTempFile (second);
			}
		}

		[TestMethod]
		public void RotateMultiple_OnException_AdvancesPastTheFailedPhoto ()
		{
			var blocked = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var next = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			try {
				var beforeNext = OrientationOf (next);
				System.IO.File.SetAttributes (blocked.AbsolutePath, FileAttributes.ReadOnly);
				var items = new IPhoto[] { new FilePhoto (blocked), new FilePhoto (next) };
				var op = new RotateMultiple (items, RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

				Assert.ThrowsExactly<RotateException> (() => op.Step ());
				Assert.AreEqual (1, op.Index);

				Assert.IsFalse (op.Step ());
				Assert.AreEqual (2, op.Index);
				Assert.AreEqual (ImageOrientations.Rotate90 (beforeNext), OrientationOf (next));
			} finally {
				System.IO.File.SetAttributes (blocked.AbsolutePath, FileAttributes.Normal);
				ImageTestHelper.DeleteTempFile (blocked);
				ImageTestHelper.DeleteTempFile (next);
			}
		}

		[TestMethod]
		public void RotateMultiple_MissingFile_ReportsThatPhoto ()
		{
			string missing = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N"), "a.jpg");
			var uri = new SafeUri (missing);
			var photo = new FilePhoto (uri);
			var op = new RotateMultiple (new IPhoto[] { photo }, RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

			var ex = Assert.ThrowsExactly<RotateException> (() => op.Step ());

			Assert.AreEqual (uri.LocalPath, ex.Path);
			Assert.AreSame (photo, op.FailedItem);
			Assert.AreEqual (1, op.Index);
			Assert.AreEqual (uri.LocalPath, op.FailedPath ());
		}

		[TestMethod]
		public void Rotate_MissingFileWithASpaceInTheName_ReportsTheDecodedPath ()
		{
			string directory = Path.Combine (Path.GetTempPath (), "fspot-rotate-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (directory);
			string path = Path.Combine (directory, "Carol Jones.jpg");
			try {
				var uri = SafeUri.FromPath (path);
				var photo = new FilePhoto (uri);
				var op = new RotateMultiple (new IPhoto[] { photo }, RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

				var ex = Assert.ThrowsExactly<RotateException> (() => op.Step ());

				Assert.AreEqual (FSpot.Resources.Lang.Strings.FileNotFonud, ex.Message);
				Assert.AreEqual (path, ex.Path);
				Assert.DoesNotContain ("%20", ex.Path);
				Assert.AreEqual (path, op.FailedPath ());
			} finally {
				Directory.Delete (directory, true);
			}
		}

		[TestMethod]
		public void Rotate_UnreadablePhotoWithASpaceInTheName_ReportsTheDecodedPath ()
		{
			string directory = Path.Combine (Path.GetTempPath (), "fspot-rotate-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (directory);
			string path = Path.Combine (directory, "Carol Jones.jpg");
			System.IO.File.WriteAllText (path, "not a photo");
			try {
				var uri = SafeUri.FromPath (path);
				var op = new RotateOperation (new FilePhoto (uri), RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

				var ex = Assert.ThrowsExactly<RotateException> (() => op.Step ());

				Assert.AreEqual (FSpot.Resources.Lang.Strings.UnableToRotateThisTypeOfPhoto, ex.Message);
				Assert.AreEqual (path, ex.Path);
				Assert.DoesNotContain ("%20", ex.Path);
				Assert.DoesNotContain ("file:", ex.Path);
			} finally {
				Directory.Delete (directory, true);
			}
		}

		[TestMethod]
		public void Rotate_MissingDirectoryWithASpaceInTheName_ReportsTheDecodedPath ()
		{
			string path = Path.Combine (Path.GetTempPath (), "fspot-rotate-" + Guid.NewGuid ().ToString ("N"), "Carol Jones", "a.jpg");
			var uri = SafeUri.FromPath (path);
			var photo = new FilePhoto (uri);
			var op = new RotateMultiple (new IPhoto[] { photo }, RotateDirection.Clockwise, Mock.Of<IThumbnailService> ());

			var ex = Assert.ThrowsExactly<RotateException> (() => op.Step ());

			Assert.AreEqual (FSpot.Resources.Lang.Strings.DirectoryNotFound, ex.Message);
			Assert.AreEqual (path, ex.Path);
			Assert.DoesNotContain ("%20", ex.Path);
		}

		[TestMethod]
		public void RotateMultiple_Empty_StopsImmediately ()
		{
			var op = new RotateMultiple (System.Array.Empty<IPhoto> (), RotateDirection.Clockwise,
				Mock.Of<IThumbnailService> ());
			Assert.IsFalse (op.Step ());
			Assert.AreEqual (0, op.Index);
		}

		static ImageOrientation OrientationOf (SafeUri uri)
		{
			using var metadata = MetadataUtils.Parse (uri);
			return metadata.ImageTag.Orientation;
		}
	}
}
