//
// MetadataTest.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TagLib;
using TagLib.Xmp;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class MetadataTest
	{
		[TestMethod]
		public void Keyword_RoundTripsInAFolderWhoseNameContainsAHash ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-meta-" + System.Guid.NewGuid ().ToString ("N"));
			string folder = Path.Combine (root, "Trip #2");
			Directory.CreateDirectory (folder);
			string source = ImageTestHelper.CreateTempFile ("taglib-sample.jpg").AbsolutePath;
			string photo = Path.Combine (folder, "photo.jpg");
			System.IO.File.Copy (source, photo, true);
			RoundTripKeyword (root, source, photo, hashFolder: true);
		}

		// '?' is illegal in a Windows file name. The hash folder above is the case Windows can create.
		[TestMethod]
		[OSCondition (ConditionMode.Exclude, OperatingSystems.Windows)]
		public void Keyword_RoundTripsInAFileWhoseNameContainsAQuestionMark ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-meta-" + System.Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string source = ImageTestHelper.CreateTempFile ("taglib-sample.jpg").AbsolutePath;
			string photo = Path.Combine (root, "q?x.jpg");
			System.IO.File.Copy (source, photo, true);
			RoundTripKeyword (root, source, photo, hashFolder: false);
		}

		static void RoundTripKeyword (string root, string source, string photo, bool hashFolder)
		{
			SafeUri uri = SafeUri.FromPath (photo);
			try {
				var file = MetadataUtils.Parse (uri);
				file.ImageTag.Keywords = new[] { "Birthday" };
				file.SaveSafely (uri, alwaysSidecar: false);
				file.Dispose ();

				var again = MetadataUtils.Parse (uri);
				Assert.AreSequenceEqual (new[] { "Birthday" }, again.ImageTag.Keywords);
				again.Dispose ();
				Assert.IsTrue (System.IO.File.Exists (photo));
				if (hashFolder) {
					Assert.Contains ("#", Path.GetDirectoryName (photo));
					Assert.IsFalse (Directory.Exists (Path.Combine (root, "Trip ")));
				} else {
					Assert.Contains ("?", Path.GetFileName (photo));
				}
			} finally {
				ImageTestHelper.DeleteTempFile (new SafeUri (source, true));
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void ValidateWithoutSidecar ()
		{
			// Tests the file in its original state
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			var xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2009-08-04T20:42:36Z", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.IsEmpty (node.Children);
			}

			Assert.AreSequenceEqual (new[] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
		}

		[TestMethod]
		public void ValidateWithSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample.xmp");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			var xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.IsNull (node);
			}

			Assert.AreSequenceEqual (new[] { "F-Spot", "metadata", "test" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}

		[TestMethod]
		public void ValidateWithBrokenSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			var xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.AreEqual ("2009-08-04T20:42:36Z", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.IsEmpty (node.Children);
			}

			Assert.AreSequenceEqual (new[] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}

		[TestMethod]
		public void ValidateWithBrokenMetadata ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg");
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNull (file);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}
	}
}