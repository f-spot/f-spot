#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;
using Banshee.Database;
using FSpot.Imaging;

namespace FSpot.Tests
{
    [TestFixture]
    public class ImageFileTests
    {
        [SetUp]
        public void Initialize () {
            GLib.GType.Init ();
        }

        [Test]
        public void CheckLoadableTypes ()
        {
            bool missing = false;

            // Test that we have loaders defined for all Taglib# parseable types.
            foreach (var key in TagLib.FileTypes.AvailableTypes.Keys) {
                Type type = TagLib.FileTypes.AvailableTypes [key];
                if (!type.IsSubclassOf (typeof (TagLib.Image.File))) {
                    continue;
                }

                var test_key = key;
                if (key.StartsWith ("taglib/")) {
                    test_key = "." + key.Substring (7);
                }

                if (!ImageFile.NameTable.ContainsKey (test_key)) {
                    Log.InformationFormat ("Missing key for {0}", test_key);
                    missing = true;
                }
            }

            Assert.IsFalse (missing, "No missing loaders for Taglib# parseable files.");
        }
    }
}
#endif
