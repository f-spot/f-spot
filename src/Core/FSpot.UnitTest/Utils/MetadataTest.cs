//
// MetadataTest.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using NUnit.Framework;

using TagLib;
using TagLib.Xmp;

namespace FSpot.Utils.Tests
{
	[TestFixture]
	public class MetadataTest
	{
		[Test]
		public void ValidateWithoutSidecar ()
		{
			// Tests the file in its original state
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2009-08-04T20:42:36Z", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}

			Assert.AreEqual (new[] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
		}

		[Test]
		public void ValidateWithSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample.xmp");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.IsNull (node);
			}

			Assert.AreEqual (new[] { "F-Spot", "metadata", "test" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}

		[Test]
		public void ValidateWithBrokenSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");

			var file = MetadataUtils.Parse (uri);
			Assert.IsNotNull (file);

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.AreEqual ("2009-08-04T20:42:36Z", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}

			Assert.AreEqual (new[] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}

		[Test]
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