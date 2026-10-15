// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot
{
	public sealed class HtmlGalleryPhoto
	{
		public HtmlGalleryPhoto (string sourcePath, string description, IEnumerable<string> tags)
		{
			SourcePath = sourcePath ?? string.Empty;
			Description = description ?? string.Empty;
			var names = new List<string> ();
			if (tags != null) {
				foreach (string tag in tags) {
					if (!string.IsNullOrEmpty (tag))
						names.Add (tag);
				}
			}
			Tags = names;
		}

		public string SourcePath { get; }
		public string Description { get; }
		public IReadOnlyList<string> Tags { get; }
	}

	// Writes a standalone HTML gallery under the folder the person chose.
	// GTK built the same tree in a temp directory and then left it there.
	public static class HtmlGallery
	{
		public const int PageSize = 16;
		public const int MediumLongerSide = 480;
		public const int ThumbLongerSide = 120;

		public delegate bool ScaleImage (string sourcePath, string destinationPath, int longerSide);

		public static void Write (string folder, string galleryName, IReadOnlyList<HtmlGalleryPhoto> photos, ScaleImage scale)
		{
			if (!StaysInFolder (folder, galleryName) || photos == null || photos.Count == 0)
				return;

			var present = new List<HtmlGalleryPhoto> ();
			foreach (HtmlGalleryPhoto photo in photos) {
				if (photo != null && !string.IsNullOrEmpty (photo.SourcePath) && File.Exists (photo.SourcePath))
					present.Add (photo);
			}
			if (present.Count == 0)
				return;

			var tags = new SortedDictionary<string, List<int>> (StringComparer.Ordinal);
			for (int i = 0; i < present.Count; i++) {
				foreach (string tag in present[i].Tags) {
					if (!tags.TryGetValue (tag, out List<int> indexes)) {
						indexes = new List<int> ();
						tags.Add (tag, indexes);
					}
					indexes.Add (i);
				}
			}
			Dictionary<string, string> stems = TagStems (tags);

			string gallery = Path.Combine (Path.GetFullPath (folder), galleryName);
			Directory.CreateDirectory (Path.Combine (gallery, "hq"));
			Directory.CreateDirectory (Path.Combine (gallery, "mq"));
			Directory.CreateDirectory (Path.Combine (gallery, "thumbs"));
			Directory.CreateDirectory (Path.Combine (gallery, "style"));
			Directory.CreateDirectory (Path.Combine (gallery, "script"));

			for (int i = 0; i < present.Count; i++) {
				string name = ImageName (i);
				string source = present[i].SourcePath;
				CopyFile (source, Path.Combine (gallery, "hq", name));
				PlaceScaled (source, Path.Combine (gallery, "mq", name), MediumLongerSide, scale);
				PlaceScaled (source, Path.Combine (gallery, "thumbs", name), ThumbLongerSide, scale);
			}

			for (int i = 0; i < present.Count; i++)
				File.WriteAllText (Path.Combine (gallery, PhotoPageName (i)), PhotoPage (galleryName, present, i, stems));

			int pages = (present.Count + PageSize - 1) / PageSize;
			for (int page = 0; page < pages; page++)
				File.WriteAllText (Path.Combine (gallery, IndexName (page)), IndexPage (galleryName, present, page, pages));

			if (tags.Count > 0) {
				File.WriteAllText (Path.Combine (gallery, "tags.html"), TagsPage (galleryName, tags, stems));
				foreach (KeyValuePair<string, List<int>> tag in tags) {
					int tagPages = (tag.Value.Count + PageSize - 1) / PageSize;
					string stem = stems[tag.Key];
					for (int page = 0; page < tagPages; page++)
						File.WriteAllText (Path.Combine (gallery, TagFile (stem, page)), TagPage (galleryName, tag.Key, stem, tag.Value, page, tagPages));
				}
			}

			CopyResource ("f-spot-simple.css", Path.Combine (gallery, "style", "f-spot-simple.css"));
			CopyResource ("f-spot-simple-white.css", Path.Combine (gallery, "style", "f-spot-simple-white.css"));
			CopyResource ("f-spot.js", Path.Combine (gallery, "script", "f-spot.js"));
		}

		public static bool StaysInFolder (string folder, string name)
		{
			if (string.IsNullOrWhiteSpace (folder) || string.IsNullOrWhiteSpace (name))
				return false;
			if (name.IndexOfAny (new[] { '/', '\\' }) >= 0 || name == "." || name == "..")
				return false;
			// '<' is a legal file name on Unix and an illegal one on Windows.
			if (name.IndexOfAny (Path.GetInvalidFileNameChars ()) >= 0)
				return false;
			string root = Path.GetFullPath (folder);
			// GetFullPath does not resolve a directory symlink that already has this name.
			return FileIdentity.IsInside (root, Path.Combine (root, name));
		}

		static void PlaceScaled (string source, string destination, int longerSide, ScaleImage scale)
		{
			if (scale != null && scale (source, destination, longerSide))
				return;
			CopyFile (source, destination);
		}

		static void CopyFile (string source, string destination)
		{
			string directory = Path.GetDirectoryName (destination);
			if (!string.IsNullOrEmpty (directory))
				Directory.CreateDirectory (directory);
			File.Copy (source, destination, true);
		}

		static void CopyResource (string name, string destination)
		{
			using (Stream input = typeof (HtmlGallery).Assembly.GetManifestResourceStream (name)) {
				if (input == null)
					throw new InvalidOperationException (name);
				using (Stream output = File.Create (destination))
					input.CopyTo (output);
			}
		}

		static string ImageName (int index)
			=> "img-" + (index + 1) + ".jpg";

		static string PhotoPageName (int index)
			=> "img-" + (index + 1) + ".html";

		public static string IndexName (int page)
			=> page == 0 ? "index.html" : "index" + page + ".html";

		public static string TagFileName (string tag, int page)
			=> TagFile (TagStem (tag), page);

		// "North Beach" and "North_Beach" sanitize to one file name, and a later
		// page (tag_North_Beach_1.html) can match another tag's first page.
		// tag_Beach_1.html and tag_beach_1.html are one file on a case-insensitive volume.
		static Dictionary<string, string> TagStems (SortedDictionary<string, List<int>> tags)
		{
			var used = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			var stems = new Dictionary<string, string> (StringComparer.Ordinal);
			foreach (KeyValuePair<string, List<int>> tag in tags) {
				int pages = (tag.Value.Count + PageSize - 1) / PageSize;
				string stem = TagStem (tag.Key);
				string unique = stem;
				int suffix = 2;
				while (StemTaken (used, unique, pages))
					unique = stem + "-" + suffix++;
				for (int page = 0; page < pages; page++)
					used.Add (TagFile (unique, page));
				stems.Add (tag.Key, unique);
			}
			return stems;
		}

		static bool StemTaken (HashSet<string> used, string stem, int pages)
		{
			for (int page = 0; page < pages; page++) {
				if (used.Contains (TagFile (stem, page)))
					return true;
			}
			return false;
		}

		static string TagStem (string tag)
		{
			var builder = new StringBuilder ("tag_");
			foreach (char c in tag ?? string.Empty) {
				if (char.IsLetterOrDigit (c) || c == '-' || c == '_' || c == '.')
					builder.Append (c);
				else
					builder.Append ('_');
			}
			return builder.ToString ();
		}

		static string TagFile (string stem, int page)
			=> page == 0 ? stem + ".html" : stem + "_" + page + ".html";

		static string PhotoPage (string galleryName, IReadOnlyList<HtmlGalleryPhoto> photos, int index, IReadOnlyDictionary<string, string> stems)
		{
			var page = new StringBuilder ();
			Open (page, galleryName, "");
			page.Append ("<div id=\"title\">").Append (Encode (galleryName)).Append ("</div>");
			page.Append ("<div class=\"navi\">");
			if (index > 0)
				Nav (page, "prev", PhotoPageName (index - 1), Strings.Prev);
			Nav (page, "index", IndexName (index / PageSize), Strings.Index);
			if (photos[index].Tags.Count > 0)
				Nav (page, "tagpage", "tags.html", Strings.Tags);
			if (index < photos.Count - 1)
				Nav (page, "next", PhotoPageName (index + 1), Strings.Next);
			page.Append ("</div>");
			page.Append ("<div class=\"photo\"><a href=\"hq/").Append (ImageName (index)).Append ("\">");
			page.Append ("<img src=\"mq/").Append (ImageName (index)).Append ("\" alt=\"#\" /></a>");
			page.Append ("<div id=\"description\">").Append (Encode (photos[index].Description)).Append ("</div></div>");
			WriteTagLinks (page, photos[index].Tags, stems);
			Close (page);
			return page.ToString ();
		}

		static string IndexPage (string galleryName, IReadOnlyList<HtmlGalleryPhoto> photos, int pageNumber, int pages)
		{
			var page = new StringBuilder ();
			Open (page, galleryName, "");
			page.Append ("<div id=\"title\">").Append (Encode (galleryName)).Append ("</div>");
			WritePageLinks (page, pages, pageNumber, IndexName, photos.AnyTag ());
			int start = pageNumber * PageSize;
			int end = Math.Min (start + PageSize, photos.Count);
			page.Append ("<div class=\"thumbs\">");
			for (int i = start; i < end; i++) {
				page.Append ("<a href=\"").Append (PhotoPageName (i)).Append ("\">");
				page.Append ("<img src=\"thumbs/").Append (ImageName (i)).Append ("\" alt=\"#\" /></a>");
			}
			page.Append ("</div>");
			Close (page);
			return page.ToString ();
		}

		static string TagsPage (string galleryName, SortedDictionary<string, List<int>> tags, IReadOnlyDictionary<string, string> stems)
		{
			var page = new StringBuilder ();
			Open (page, galleryName, " " + Strings.Tags);
			page.Append ("<div id=\"title\">").Append (Encode (galleryName)).Append (' ').Append (Encode (Strings.Tags)).Append ("</div>");
			page.Append ("<a href=\"index.html\">").Append (Encode (Strings.Index)).Append ("</a>");
			WriteTagLinks (page, tags.Keys, stems);
			Close (page);
			return page.ToString ();
		}

		static string TagPage (string galleryName, string tag, string stem, IReadOnlyList<int> indexes, int pageNumber, int pages)
		{
			var page = new StringBuilder ();
			Open (page, galleryName, ": " + tag);
			page.Append ("<div id=\"title\">").Append (Encode (galleryName)).Append (": ").Append (Encode (tag)).Append ("</div>");
			page.Append ("<a href=\"index.html\">").Append (Encode (Strings.Index)).Append ("</a>");
			page.Append ("<a href=\"tags.html\">").Append (Encode (Strings.Tags)).Append ("</a>");
			WritePageLinks (page, pages, pageNumber, pageIndex => TagFile (stem, pageIndex), false);
			int start = pageNumber * PageSize;
			int end = Math.Min (start + PageSize, indexes.Count);
			page.Append ("<div class=\"thumbs\">");
			for (int i = start; i < end; i++) {
				int photo = indexes[i];
				page.Append ("<a href=\"").Append (PhotoPageName (photo)).Append ("\">");
				page.Append ("<img src=\"thumbs/").Append (ImageName (photo)).Append ("\" alt=\"#\" /></a>");
			}
			page.Append ("</div>");
			Close (page);
			return page.ToString ();
		}

		static void WritePageLinks (StringBuilder page, int pages, int current, Func<int, string> path, bool tags)
		{
			page.Append ("<div class=\"navi\">");
			if (tags) {
				page.Append ("<div class=\"navipage\"><a href=\"tags.html\">").Append (Encode (Strings.Tags)).Append ("</a></div>");
			}
			page.Append ("<div class=\"navilabel\">").Append (Encode (Strings.PageColon)).Append ("</div>");
			for (int i = 0; i < pages; i++) {
				page.Append ("<div class=\"").Append (i == current ? "navipage-current" : "navipage").Append ("\">");
				page.Append ("<a href=\"").Append (path (i)).Append ("\">").Append (i + 1).Append ("</a></div>");
			}
			page.Append ("</div>");
		}

		static void WriteTagLinks (StringBuilder page, IEnumerable<string> tags, IReadOnlyDictionary<string, string> stems)
		{
			page.Append ("<div id=\"tagbox\"><ul>");
			foreach (string tag in tags) {
				page.Append ("<li class=\"tag\"><a href=\"").Append (TagFile (stems[tag], 0)).Append ("\">");
				page.Append ("<span>").Append (Encode (tag)).Append ("</span></a></li>");
			}
			page.Append ("</ul></div>");
		}

		static void Open (StringBuilder page, string galleryName, string titleExtension)
		{
			page.Append ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">\n");
			page.Append ("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\"><head>");
			page.Append ("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />");
			page.Append ("<title>").Append (Encode (galleryName)).Append (Encode (titleExtension)).Append ("</title>");
			page.Append ("<link type=\"text/css\" rel=\"stylesheet\" href=\"style/f-spot-simple.css\" title=\"").Append (Encode (Strings.Dark)).Append ("\" />");
			page.Append ("<link type=\"text/css\" rel=\"alternate stylesheet\" href=\"style/f-spot-simple-white.css\" title=\"").Append (Encode (Strings.Light)).Append ("\" />");
			page.Append ("<script src=\"script/f-spot.js\" type=\"text/javascript\"></script>");
			page.Append ("</head><body onload=\"checkForTheme()\"><div class=\"container1\"><div class=\"header\">");
		}

		static void Close (StringBuilder page)
		{
			page.Append ("<div class=\"footer\">").Append (Encode (Strings.GalleryGeneratedBy)).Append (" ");
			page.Append ("<a href=\"http://f-spot.org\">").Append (Encode (FSpotConfiguration.Package)).Append (' ');
			page.Append (Encode (FSpotConfiguration.Version)).Append ("</a></div>");
			page.Append ("</div></body></html>");
		}

		static void Nav (StringBuilder page, string id, string href, string label)
		{
			page.Append ("<div id=\"").Append (id).Append ("\"><a href=\"").Append (href).Append ("\">");
			page.Append (Encode (label)).Append ("</a></div>");
		}

		static string Encode (string text)
			=> WebUtility.HtmlEncode (text ?? string.Empty);

		static bool AnyTag (this IReadOnlyList<HtmlGalleryPhoto> photos)
		{
			foreach (HtmlGalleryPhoto photo in photos) {
				if (photo.Tags.Count > 0)
					return true;
			}
			return false;
		}
	}
}
