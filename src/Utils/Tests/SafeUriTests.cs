#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;
using FSpot;

namespace FSpot.Utils.Tests
{
    [TestFixture]
    public class SafeUriTests
    {
        static SafeUriTest[] tests = new SafeUriTest[] {
            new SafeUriTest () {
                Uri = "https://bugzilla.gnome.org/process_bug.cgi",
                IsURI = true,
                AbsoluteUri = "https://bugzilla.gnome.org/process_bug.cgi",
                Extension = ".cgi",
                BaseUri = "https://bugzilla.gnome.org",
                Filename = "process_bug.cgi",
                FilenameWithoutExtension = "process_bug"

            },
            new SafeUriTest () {
                Uri = "file:///home/ruben/Desktop/F-Spot Vision.pdf",
                IsURI = true,
                AbsoluteUri = "file:///home/ruben/Desktop/F-Spot Vision.pdf",
                Extension = ".pdf",
                BaseUri = "file:///home/ruben/Desktop",
                Filename = "F-Spot Vision.pdf",
                FilenameWithoutExtension = "F-Spot Vision"
            },
            new SafeUriTest () {
                Uri = "/home/ruben/Projects/f-spot/",
                IsURI = false,
                AbsoluteUri = "file:///home/ruben/Projects/f-spot/",
                Extension = "",
                BaseUri = "file:///home/ruben/Projects/f-spot",
                Filename = "",
                FilenameWithoutExtension = ""
            },
            new SafeUriTest () {
                Uri = "/home/ruben/Projects/f-spot/README",
                IsURI = false,
                AbsoluteUri = "file:///home/ruben/Projects/f-spot/README",
                Extension = "",
                BaseUri = "file:///home/ruben/Projects/f-spot",
                Filename = "README",
                FilenameWithoutExtension = "README"
            }
        };

        [Test]
        public void TestFileUris ()
        {
            foreach (var test in tests) {
                var suri = new SafeUri (test.Uri, test.IsURI);
                Assert.AreEqual (suri.AbsoluteUri, test.AbsoluteUri, String.Format("AbsoluteUri for {0}", test.Uri));
                Assert.AreEqual (suri.GetExtension (), test.Extension, String.Format("Extension for {0}", test.Uri));
                Assert.AreEqual (suri.GetBaseUri ().ToString (), test.BaseUri, String.Format("BaseUri for {0}", test.Uri));
                Assert.AreEqual (suri.GetFilename (), test.Filename, String.Format("Filename for {0}", test.Uri));
                Assert.AreEqual (suri.GetFilenameWithoutExtension (), test.FilenameWithoutExtension, String.Format("FilenameWithoutExtension for {0}", test.Uri));
            }
        }
    }

    internal class SafeUriTest
    {
        public string Uri { get; set; }
        public bool IsURI { get; set; }
        public string AbsoluteUri { get; set; }
        public string Extension { get; set; }
        public string BaseUri { get; set; }
        public string Filename { get; set; }
        public string FilenameWithoutExtension { get; set; }
    }
}
#endif
