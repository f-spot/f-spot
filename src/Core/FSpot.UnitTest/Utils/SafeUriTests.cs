//
// SafeUriTests.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

using NUnit.Framework;

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
			},
			new SafeUriTest () {
				Uri = "gphoto2://[usb:002,014]/",
				AbsoluteUri = "gphoto2://[usb:002,014]/",
				Extension = "",
				BaseUri = "gphoto2://[usb:002,014]",
				Filename = "",
				FilenameWithoutExtension = ""
			}
		};

		[Test]
		public void TestFileUris ()
		{
			foreach (var test in tests) {
				var suri = new SafeUri (test.Uri);
				Assert.AreEqual (suri.AbsoluteUri, test.AbsoluteUri, string.Format ("AbsoluteUri for {0}", test.Uri));
				Assert.AreEqual (suri.GetExtension (), test.Extension, string.Format ("Extension for {0}", test.Uri));
				Assert.AreEqual (suri.GetBaseUri ().ToString (), test.BaseUri, string.Format ("BaseUri for {0}", test.Uri));
				Assert.AreEqual (suri.GetFilename (), test.Filename, string.Format ("Filename for {0}", test.Uri));
				Assert.AreEqual (suri.GetFilenameWithoutExtension (), test.FilenameWithoutExtension, string.Format ("FilenameWithoutExtension for {0}", test.Uri));
			}
		}

		[Test]
		public void TestReplaceExtension ()
		{
			var uri = new SafeUri ("file:///test/image.jpg", true);
			var goal = new SafeUri ("file:///test/image.xmp", true);

			Assert.AreEqual (goal, uri.ReplaceExtension (".xmp"));
		}
	}

	class SafeUriTest
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