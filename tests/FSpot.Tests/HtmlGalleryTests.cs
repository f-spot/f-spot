// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class HtmlGalleryTests
	{
		[TestMethod]
		public void Write_CopiesTheFullPhotoAndEscapesTheTitle ()
		{
			string root = NewRoot ();
			string source = Path.Combine (root, "a#b.jpg");
			File.WriteAllText (source, "full-bytes");
			string dest = Path.Combine (root, "chosen");
			Directory.CreateDirectory (dest);
			try {
				HtmlGallery.Write (dest, "Summer <1>", new[] {
					new HtmlGalleryPhoto (source, "A < B", new[] { "Beach" })
				}, null);

				string gallery = Path.Combine (dest, "Summer <1>");
				Assert.AreEqual ("full-bytes", File.ReadAllText (Path.Combine (gallery, "hq", "img-1.jpg")));
				string index = File.ReadAllText (Path.Combine (gallery, "index.html"));
				Assert.Contains ("img-1.html", index);
				Assert.Contains ("Summer &lt;1&gt;", index);
				Assert.DoesNotContain ("Summer <1>", index);
				string page = File.ReadAllText (Path.Combine (gallery, "img-1.html"));
				Assert.Contains ("hq/img-1.jpg", page);
				Assert.Contains ("mq/img-1.jpg", page);
				Assert.Contains ("A &lt; B", page);
				Assert.IsFalse (Directory.Exists (Path.Combine (root, "f-spot-original")));
				Assert.IsEmpty (Directory.GetDirectories (root, "f-spot-original*", SearchOption.AllDirectories));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Write_LinksTheSeventeenthPhotoFromTheSecondIndex ()
		{
			string root = NewRoot ();
			string dest = Path.Combine (root, "chosen");
			Directory.CreateDirectory (dest);
			var photos = new List<HtmlGalleryPhoto> ();
			for (int i = 1; i <= 17; i++) {
				string source = Path.Combine (root, "p" + i + ".jpg");
				File.WriteAllText (source, "p" + i);
				photos.Add (new HtmlGalleryPhoto (source, "plain", Array.Empty<string> ()));
			}
			try {
				HtmlGallery.Write (dest, "Trip", photos, null);

				string gallery = Path.Combine (dest, "Trip");
				string first = File.ReadAllText (Path.Combine (gallery, "index.html"));
				string second = File.ReadAllText (Path.Combine (gallery, "index1.html"));
				Assert.Contains ("img-1.html", first);
				Assert.Contains ("img-16.html", first);
				Assert.DoesNotContain ("img-17.html", first);
				Assert.Contains ("img-17.html", second);
				Assert.AreEqual ("p17", File.ReadAllText (Path.Combine (gallery, "hq", "img-17.jpg")));
				Assert.IsFalse (File.Exists (Path.Combine (gallery, "index0.html")));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Write_RefusesANameThatLeavesTheChosenFolder ()
		{
			string root = NewRoot ();
			string source = Path.Combine (root, "a.jpg");
			File.WriteAllText (source, "full-bytes");
			string dest = Path.Combine (root, "chosen");
			Directory.CreateDirectory (dest);
			string outside = Path.Combine (root, "out");
			try {
				HtmlGallery.Write (dest, "../out", new[] {
					new HtmlGalleryPhoto (source, "plain", Array.Empty<string> ())
				}, null);

				Assert.IsFalse (Directory.Exists (outside));
				Assert.IsEmpty (Directory.GetDirectories (dest));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Write_ScalesTheMediumAndThumbCopiesOnly ()
		{
			string root = NewRoot ();
			string source = Path.Combine (root, "a.jpg");
			File.WriteAllText (source, "full-bytes");
			string dest = Path.Combine (root, "chosen");
			Directory.CreateDirectory (dest);
			var edges = new List<int> ();
			try {
				HtmlGallery.Write (dest, "Trip", new[] {
					new HtmlGalleryPhoto (source, "plain", Array.Empty<string> ())
				}, (from, to, longerSide) => {
					edges.Add (longerSide);
					File.WriteAllText (to, "edge-" + longerSide);
					return true;
				});

				string gallery = Path.Combine (dest, "Trip");
				Assert.AreEqual ("full-bytes", File.ReadAllText (Path.Combine (gallery, "hq", "img-1.jpg")));
				Assert.AreEqual ("edge-480", File.ReadAllText (Path.Combine (gallery, "mq", "img-1.jpg")));
				Assert.AreEqual ("edge-120", File.ReadAllText (Path.Combine (gallery, "thumbs", "img-1.jpg")));
				Assert.AreSequenceEqual (new[] { 480, 120 }, edges);
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Write_CopiesTheStyleSheetAndEscapesATagName ()
		{
			string root = NewRoot ();
			string source = Path.Combine (root, "a.jpg");
			File.WriteAllText (source, "full-bytes");
			string dest = Path.Combine (root, "chosen");
			Directory.CreateDirectory (dest);
			try {
				HtmlGallery.Write (dest, "Trip", new[] {
					new HtmlGalleryPhoto (source, "plain", new[] { "a<b", "North Beach" })
				}, null);

				string gallery = Path.Combine (dest, "Trip");
				string style = File.ReadAllText (Path.Combine (gallery, "style", "f-spot-simple.css"));
				Assert.Contains (".header", style);
				string script = File.ReadAllText (Path.Combine (gallery, "script", "f-spot.js"));
				Assert.Contains ("setActiveStyleSheet", script);
				string tags = File.ReadAllText (Path.Combine (gallery, "tags.html"));
				Assert.Contains ("a&lt;b", tags);
				Assert.Contains ("tag_North_Beach.html", tags);
				Assert.IsTrue (File.Exists (Path.Combine (gallery, "tag_North_Beach.html")));
				Assert.IsFalse (File.Exists (Path.Combine (gallery, "tag_a<b.html")));
				string tagPage = File.ReadAllText (Path.Combine (gallery, "tag_a_b.html"));
				Assert.Contains ("img-1.html", tagPage);
				Assert.Contains ("thumbs/img-1.jpg", tagPage);
			} finally {
				Directory.Delete (root, true);
			}
		}

		static string NewRoot ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-gallery-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			return root;
		}
	}
}
