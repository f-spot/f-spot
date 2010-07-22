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
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample.xmp");
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };

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

            Assert.AreEqual (new string [] { "F-Spot", "metadata", "test" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }

        [Test]
        public void ValidateWithBrokenSidecar ()
        {
            // Tests the file with a sidecar
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = ImageTestHelper.CopySidecarToTest (uri, "taglib-sample-broken.xmp");
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };

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

            Assert.AreEqual (new string [] { "Kirche Sulzbach" }, file.ImageTag.Keywords);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }

        [Test]
        public void TestSidecarWrite ()
        {
            var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
            var sidecar_uri = uri.ReplaceExtension (".xmp");
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };
            Assert.IsTrue (sidecar_uri.ToString ().EndsWith (".xmp"));

            var sidecar_file = GLib.FileFactory.NewForUri (sidecar_uri);
            Assert.IsFalse (sidecar_file.Exists);

            var file = File.Create (res) as TagLib.Image.File;
            Assert.IsNotNull (file);

            file.ImageTag.Keywords = new string [] { "Kirche Aarschot" };

            // Validate writing of the sidecar
            bool success = file.SaveXmpSidecar (sidecar_res);
            Assert.IsTrue (success);
            Assert.IsTrue (sidecar_file.Exists);

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
            var read_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };
            using (var stream = read_res.ReadStream) {
                using (var reader = new System.IO.StreamReader (stream)) {
                    written = reader.ReadToEnd ();
                }
            }
            Assert.AreEqual (target, written);

            // Check that the file is unchanged
            file = File.Create (res) as TagLib.Image.File;
            var keywords = file.ImageTag.Keywords;
            Assert.AreEqual (new string [] { "Kirche Sulzbach" }, keywords);

            ImageTestHelper.DeleteTempFile (uri);
            ImageTestHelper.DeleteTempFile (sidecar_uri);
        }
    }
}
#endif
