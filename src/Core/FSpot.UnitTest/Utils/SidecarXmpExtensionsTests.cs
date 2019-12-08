//
// SidecarXmpExtensionsTests.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
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

using Shouldly;

using TagLib;
using TagLib.Xmp;

namespace FSpot.Utils.Tests
{
	[TestFixture]
	public class SidecarXmpExtensionsTests
	{
		[Test]
		public void ValidateWithoutSidecar ()
		{
			// Tests the file in its original state
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var res = new TagLibFileAbstraction { Uri = uri };

			var file = File.Create (res) as TagLib.Image.File;
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

			Assert.AreEqual (new string[] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
		}

		[Test]
		public void ValidateWithSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var res = new TagLibFileAbstraction { Uri = uri };
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample.xmp");
			var sidecar_res = new TagLibFileAbstraction { Uri = sidecar_uri };

			var file = File.Create (res) as TagLib.Image.File;
			Assert.IsNotNull (file);

			// Override by sidecar file
			bool success = file.ParseXmpSidecar (sidecar_res);
			Assert.IsTrue (success);

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
				Assert.IsNull (node);
			}

			Assert.AreEqual (new string[] { "F-Spot", "metadata", "test" }, file.ImageTag.Keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}

		[Test]
		public void ValidateWithBrokenSidecar ()
		{
			// Tests the file with a sidecar
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var res = new TagLibFileAbstraction { Uri = uri };
			var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");
			var sidecar_res = new TagLibFileAbstraction { Uri = sidecar_uri };

			var file = File.Create (res) as TagLib.Image.File;
			Assert.IsNotNull (file);

			// Override by sidecar file should fail
			bool success = file.ParseXmpSidecar (sidecar_res);
			Assert.IsFalse (success);

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
		public void TestSidecarWrite ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			var res = new TagLibFileAbstraction { Uri = uri };
			Assert.True (System.IO.File.Exists (uri.AbsolutePath));

			var sidecar_uri = uri.ReplaceExtension (".xmp");
			var sidecar_res = new TagLibFileAbstraction { Uri = sidecar_uri };
			sidecar_uri.ToString ().ShouldEndWith (".xmp");
			System.IO.File.Create (sidecar_uri.AbsolutePath).Dispose ();
			Assert.True(System.IO.File.Exists (sidecar_uri.AbsolutePath));


			var file = File.Create (res) as TagLib.Image.File;
			Assert.IsNotNull (file);

			file.ImageTag.Keywords = new[] { "Kirche Aarschot" };

			// Validate writing of the sidecar
			bool success = file.SaveXmpSidecar (sidecar_res);
			Assert.IsTrue (success);
			Assert.IsTrue (System.IO.File.Exists (sidecar_uri.AbsolutePath));

			var target = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xm"
				+ "lns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"
				+ "<rdf:Description MicrosoftPhoto:DateAcquired=\"2009-08-0"
				+ "4T20:42:36Z\" xmp:creatortool=\"Microsoft Windows Photo "
				+ "Gallery 6.0.6001.18000\" tiff:software=\"Microsoft Windo"
				+ "ws Photo Gallery 6.0.6001.18000\" tiff:Orientation=\"1\""
				+ " MicrosoftPhoto:Rating=\"1\" xmp:Rating=\"1\" xmlns:tiff"
				+ "=\"http://ns.adobe.com/tiff/1.0/\" xmlns:xmp=\"http://ns"
				+ ".adobe.com/xap/1.0/\" xmlns:MicrosoftPhoto=\"http://ns.m"
				+ "icrosoft.com/photo/1.0/\"><MicrosoftPhoto:LastKeywordXMP"
				+ "><rdf:Bag><rdf:li>Kirche Sulzbach</rdf:li></rdf:Bag></Mi"
				+ "crosoftPhoto:LastKeywordXMP><dc:subject xmlns:dc=\"http:"
				+ "//purl.org/dc/elements/1.1/\"><rdf:Bag><rdf:li>Kirche Aa"
				+ "rschot</rdf:li></rdf:Bag></dc:subject></rdf:Description>"
				+ "</rdf:RDF></x:xmpmeta>";

			string written;
			var read_res = new TagLibFileAbstraction { Uri = sidecar_uri };
			using (var stream = read_res.ReadStream) {
				using var reader = new System.IO.StreamReader (stream);
				written = reader.ReadToEnd ();
			}
			Assert.AreEqual (target, written);

			// Check that the file is unchanged
			file = File.Create (res) as TagLib.Image.File;
			var keywords = file.ImageTag.Keywords;
			Assert.AreEqual (new[] { "Kirche Sulzbach" }, keywords);

			ImageTestHelper.DeleteTempFile (uri);
			ImageTestHelper.DeleteTempFile (sidecar_uri);
		}
	}
}