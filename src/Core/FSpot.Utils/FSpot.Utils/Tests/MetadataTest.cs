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
    public class MetadataTest
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

            var file = Metadata.Parse (uri);
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

            Assert.AreEqual (new string [] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
        }

        [Test]
        public void ValidateWithSidecar ()
        {
            // Tests the file with a sidecar
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample.xmp");

            var file = Metadata.Parse (uri);
            Assert.IsNotNull (file);

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

        [Test]
        public void ValidateWithBrokenSidecar ()
        {
            // Tests the file with a sidecar
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");

            var file = Metadata.Parse (uri);
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

            Assert.AreEqual (new string [] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }

        [Test]
        public void ValidateWithBrokenMetadata ()
        {
            // Tests the file with a sidecar
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg");
            var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");

            var file = Metadata.Parse (uri);
            Assert.IsNull (file);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }
    }
}
#endif
