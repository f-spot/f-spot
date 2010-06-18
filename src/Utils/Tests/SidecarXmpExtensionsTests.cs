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
    [TestFixture]
    public class SidecarXmpExtensionsTests
    {
        [SetUp]
        public void Initialize () {
            GLib.GType.Init ();
        }

        [Test]
        public void ValidateWithoutSidecar ()
        {
            // Tests the file in its original state
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var res = new GIOTagLibFileAbstraction () { Uri = uri };

            var file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

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

            Assert.AreEqual (new string [] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
        }

        [Test]
        public void ValidateWithSidecar ()
        {
            // Tests the file with a sidecar
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = CopySidecarToTest (uri);
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };

            var file = File.Create (res) as TagLib.Image.File;
            Assert.IsTrue (file != null);

            // Override by sidecar file
            file.ParseXmpSidecar (sidecar_res);

            XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
            // Xmp.MicrosoftPhoto_1_.DateAcquired (XmpText/20) "2009-08-04T20:42:36Z"
            {
                var node = xmp.NodeTree;
                node = node.GetChild (XmpTag.MS_PHOTO_NS, "DateAcquired");
                Assert.IsNull (node);
            }

            Assert.AreEqual (new string [] { "F-Spot", "metadata", "test" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }

        SafeUri CopySidecarToTest (SafeUri uri)
        {
            var target = uri.GetBaseUri ().Append (uri.GetFilenameWithoutExtension () + ".xmp");

            var orig_uri = new SafeUri (Environment.CurrentDirectory + "/../tests/data/taglib-sample.xmp");
            var file = GLib.FileFactory.NewForUri (orig_uri);
            var file2 = GLib.FileFactory.NewForUri (target);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);
            return target;
        }
    }
}
#endif
