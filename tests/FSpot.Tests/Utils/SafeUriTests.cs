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

using System;
using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
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

		[TestMethod]
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

		[TestMethod]
		public void TestReplaceExtension ()
		{
			var uri = new SafeUri ("file:///test/image.jpg", true);
			var goal = new SafeUri ("file:///test/image.xmp", true);

			Assert.AreEqual (goal, uri.ReplaceExtension (".xmp"));
		}

		[TestMethod]
		public void FromPath_RoundTripsHashQuestionMarkAndALiteralPercent ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-safeuri-" + Guid.NewGuid ().ToString ("n"));
			string hash = Path.Combine (root, "#Photos");
			string trip = Path.Combine (hash, "Trip #2", "a.jpg");
			string query = Path.Combine (root, "q?x");
			string percent = Path.Combine (root, "a%20b");

			AssertRoundTrip (SafeUri.FromPath (hash), hash);
			AssertRoundTrip (SafeUri.FromPath (trip), trip);
			AssertRoundTrip (new SafeUri (query), query);
			AssertRoundTrip (new SafeUri (percent), percent);
			AssertRoundTrip (new SafeUri (Path.Combine (root, "My Photos", "a.jpg")), Path.Combine (root, "My Photos", "a.jpg"));

			SafeUri appended = new SafeUri (hash).Append ("Trip #2.jpg");
			Assert.AreEqual ("Trip #2.jpg", appended.GetFilename ());
			AssertRoundTrip (appended, Path.Combine (hash, "Trip #2.jpg"));
		}

		[TestMethod]
		public void AFileUriWithARawHashOrQuestionMarkKeepsTheWholePath ()
		{
			var hash = new SafeUri ("file:///tmp/Trip #2/a.jpg", true);
			var query = new SafeUri ("file:///tmp/q?x/a.jpg", true);

			Assert.AreEqual ("a.jpg", hash.GetFilename ());
			Assert.AreEqual ("a.jpg", query.GetFilename ());
			Assert.Contains ("Trip #2", hash.AbsolutePath);
			Assert.Contains ("q?x", query.AbsolutePath);
		}

		[TestMethod]
		public void AFileUnderTheFilesystemRootStaysOnThatPath ()
		{
			SafeUri root = SafeUri.FromPath (Path.DirectorySeparatorChar.ToString ());
			SafeUri child = root.Append ("keep.jpg");

			Assert.AreEqual ("keep.jpg", child.GetFilename ());
			Assert.AreEqual (Path.Combine (root.AbsolutePath, "keep.jpg"), child.AbsolutePath);
		}

		[TestMethod]
		public void ADriveOrUncPathKeepsAHashInTheFileName ()
		{
			SafeUri drive = new SafeUri ("C:\\Trip #2\\a.jpg");
			SafeUri unc = new SafeUri ("\\\\server\\share\\Trip #2\\a.jpg");

			Assert.AreEqual ("a.jpg", drive.GetFilename ());
			Assert.AreEqual ("a.jpg", unc.GetFilename ());
			Assert.Contains ("Trip #2", drive.AbsolutePath);
			Assert.Contains ("Trip #2", unc.AbsolutePath);
			Assert.Contains ("server", unc.AbsolutePath);
		}

		[TestMethod]
		public void ExtensionComesFromTheFileName ()
		{
			var dottedFolder = new SafeUri ("file:///photos/2020.05/img", true);
			var jpeg = new SafeUri ("file:///photos/2020.05/a.jpg", true);
			SafeUri encoded = new SafeUri ("file:///tmp/database/", true).Append ("sample%20(Modified).jpg");

			Assert.AreEqual (string.Empty, dottedFolder.GetExtension ());
			Assert.AreEqual (".jpg", jpeg.GetExtension ());
			Assert.AreEqual (".jpg", encoded.GetExtension ());
		}

		[TestMethod]
		public void AnOrdinaryPathMatchesTheNameAppendedToItsFolder ()
		{
			SafeUri folder = SafeUri.FromPath ("/home/u/My Photos");
			SafeUri edited = folder.Append ("IMG (1).jpg");
			SafeUri imported = SafeUri.FromPath ("/home/u/My Photos/IMG (1).jpg");

			Assert.AreEqual (imported, edited);
			Assert.Contains ("My Photos", imported.AbsolutePath);
			Assert.Contains ("IMG (1).jpg", imported.AbsolutePath);
		}

		[TestMethod]
		public void ABackslashInAPosixNameStaysInTheFileName ()
		{
			SafeUri uri = SafeUri.FromPath ("/tmp/x/a\\b.jpg");

			// On Windows '\' separates folders, so this string names b.jpg.
			// On POSIX it is one file whose name contains a backslash.
			if (Path.DirectorySeparatorChar == '\\') {
				Assert.AreEqual ("b.jpg", uri.GetFilename ());
				return;
			}

			Assert.AreEqual ("a\\b.jpg", uri.GetFilename ());
			Assert.Contains ("a\\b.jpg", uri.AbsolutePath);
		}

		[TestMethod]
		public void ASingleSlashFileUriNamesThePath ()
		{
			var uri = new SafeUri ("file:/home/u/x.jpg");
			var upper = new SafeUri ("FILE:/home/u/x.jpg");

			Assert.AreEqual ("/home/u/x.jpg", uri.AbsolutePath.Replace ('\\', '/'));
			Assert.AreEqual ("/home/u/x.jpg", upper.AbsolutePath.Replace ('\\', '/'));
			Assert.AreEqual ("x.jpg", uri.GetFilename ());
		}

		[TestMethod]
		public void ALeadingOrTrailingDotIsNotAnExtension ()
		{
			var hidden = new SafeUri ("file:///photos/.hidden", true);
			var trailing = new SafeUri ("file:///photos/name.", true);

			Assert.AreEqual (string.Empty, hidden.GetExtension ());
			Assert.AreEqual (".hidden", hidden.GetFilenameWithoutExtension ());
			Assert.AreEqual (".hidden.xmp", hidden.ReplaceExtension (".xmp").GetFilename ());
			Assert.AreEqual (string.Empty, trailing.GetExtension ());
			Assert.AreEqual ("name.", trailing.GetFilenameWithoutExtension ());
		}

		[TestMethod]
		public void AnEncodedCatalogFilenameIsNotEscapedAgain ()
		{
			SafeUri uri = new SafeUri ("file:///tmp/database/", true).Append ("sample%20(Modified).jpg");

			Assert.AreEqual ("sample%20(Modified).jpg", uri.GetFilename ());
			Assert.Contains ("sample (Modified).jpg", uri.AbsolutePath.Replace ('\\', '/'));
		}

		static void AssertRoundTrip (SafeUri uri, string path)
		{
			Assert.AreEqual (path.Replace ('\\', '/'), uri.AbsolutePath.Replace ('\\', '/'));
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