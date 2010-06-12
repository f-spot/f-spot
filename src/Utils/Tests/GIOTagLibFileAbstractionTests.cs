#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;
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
        static bool initialized = false;
        static void Initialize () {
            GLib.GType.Init ();
            initialized = true;
        }

        [Test]
        public void StraightIOTest ()
        {
            var uri = CreateTempFile ();

            var file = File.Create (uri.AbsolutePath) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ChangeMetadata (file);

            file = File.Create (uri.AbsolutePath) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ValidateChangedMetadata (file);

            DeleteTempFile (uri);
        }

        [Test]
        public void GIOTest ()
        {
            var uri = CreateTempFile ();

            var res = new GIOTagLibFileAbstraction () { Uri = uri };

            var file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ChangeMetadata (file);

            file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            Validate (file);
            ValidateChangedMetadata (file);

            DeleteTempFile (uri);
        }

        private SafeUri CreateTempFile ()
        {
            if (!initialized)
                Initialize ();

            var uri = new SafeUri (Environment.CurrentDirectory + "/../tests/data/taglib-sample.jpg");
            var file = GLib.FileFactory.NewForUri (uri);

            var tmp = System.IO.Path.GetTempFileName ()+".jpg"; // hack!
            var uri2 = new SafeUri (tmp);
            var file2 = GLib.FileFactory.NewForUri (uri2);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);
            return uri2;
        }

        private void DeleteTempFile (SafeUri uri)
        {
            var file = GLib.FileFactory.NewForUri (uri);
            file.Delete ();
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
#endif
