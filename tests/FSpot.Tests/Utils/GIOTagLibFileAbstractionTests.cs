//
// GIOTagLibFileAbstractionTests.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
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

using Hyena;
using NUnit.Framework;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using TagLib.Xmp;

namespace FSpot.Utils.Tests
{
    // This is a trimmed down test case from Taglib# to test the file abstraction.
    [TestFixture]
    public class GIOTagLibFileAbstractionTests
    {
		[SetUp]
        public void Initialize () {
            GLib.GType.Init ();
        }

        [Test]
        public void StraightIOTest ()
        {
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");

            var file = File.Create (uri.AbsolutePath) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ChangeMetadata (file);

            file = File.Create (uri.AbsolutePath) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ValidateChangedMetadata (file);

            ImageTestHelper.DeleteTempFile (uri);
        }

        [Test]
        public void GIOTest ()
        {
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");

            var res = new GIOTagLibFileAbstraction () { Uri = uri };

            var file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ChangeMetadata (file);

            file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ValidateChangedMetadata (file);

            ImageTestHelper.DeleteTempFile (uri);
        }

        private void Validate (TagLib.Image.File file)
        {
            // Note: these don't correspond to the image data, only to the metadata. We hacked the file for size.
			Assert.AreEqual (2000, file.Properties.PhotoWidth);
			Assert.AreEqual (3008, file.Properties.PhotoHeight);
			Assert.AreEqual (96, file.Properties.PhotoQuality);

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010F (Make/Ascii/18) "NIKON CORPORATION"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NIKON CORPORATION", (entry as StringIFDEntry).Value);
			}

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
        }

        private void ChangeMetadata (TagLib.Image.File file)
        {
            file.ImageTag.Comment = "Testing!";
            file.ImageTag.Keywords = new string [] { "One", "Two", "Three" };
            file.Save ();
        }

        private void ValidateChangedMetadata (TagLib.Image.File file)
        {
            Assert.AreEqual ("Testing!", file.ImageTag.Comment);
            Assert.AreEqual (new string [] { "One", "Two", "Three" }, file.ImageTag.Keywords);
        }
    }
}