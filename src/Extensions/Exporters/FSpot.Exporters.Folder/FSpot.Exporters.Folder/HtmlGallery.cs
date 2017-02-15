//
// HtmlGallery.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/*
 * Copyright (C) 2005 Alessandro Gervaso <gervystar@gervystar.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301
 */

//This should be used to export the selected pics to an original gallery
//located on a GIO location.

using System;
using System.IO;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Core;
using FSpot.Settings;

namespace FSpot.Exporters.Folder
{
	class HtmlGallery : FolderGallery
	{
		int perpage = 16;
		string stylesheet = "f-spot-simple.css";
		string altstylesheet = "f-spot-simple-white.css";
		string javascript = "f-spot.js";

		//Note for translators: light as clear, opposite as dark
		static string light = Catalog.GetString("Light");
		static string dark = Catalog.GetString("Dark");

		List<string> allTagNames = new List<string> ();
		Dictionary<string,Tag> allTags = new Dictionary<string, Tag> ();
		Dictionary<string, List<int>> tagSets = new Dictionary<string, List<int>> ();

		public HtmlGallery (IBrowsableCollection selection, string path, string name) : base (selection, path, name)
		{
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 480, 320, false),
							 new ScaleRequest ("thumbs", 120, 90, false) };
		}

		protected override string ImageName (int photo_index)
		{
			return string.Format ("img-{0}.jpg", photo_index + 1);
		}

		public override void GenerateLayout ()
		{
			if (Collection.Count == 0)
				return;

			base.GenerateLayout ();

			IPhoto [] photos = Collection.Items;

			int i;
			for (i = 0; i < photos.Length; i++)
				SavePhotoHtmlIndex (i);

			for (i = 0; i < PageCount; i++)
				SaveHtmlIndex (i);

			if (ExportTags) {
				// identify tags present in these photos
				i = 0;
				foreach (IPhoto photo in photos) {
					foreach (var tag in photo.Tags) {
						if (!tagSets.ContainsKey (tag.Name)) {
							tagSets.Add (tag.Name, new List<int> ());
							allTags.Add (tag.Name, tag);
						}
						tagSets [tag.Name].Add (i);
					}
					i++;
				}
				allTagNames = new List<string> (tagSets.Keys);
				allTagNames.Sort ();

				// create tag pages
				SaveTagsPage ();
				foreach (string tag in allTagNames) {
					for (i = 0; i < TagPageCount (tag); i++)
						SaveTagIndex (tag, i);
				}
			}

			if (ExportTags && ExportTagIcons) {
				SaveTagIcons ();
			}

			MakeDir (SubdirPath ("style"));
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
			using (Stream s = assembly.GetManifestResourceStream (stylesheet)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("style", stylesheet), System.IO.FileMode.Create)) {

					byte [] buffer = new byte [8192];
					int n;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);

				}
			}
			/* quick and stupid solution
			   this should have been iterated over an array of stylesheets, really
			*/
			using (Stream s = assembly.GetManifestResourceStream (altstylesheet)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("style", altstylesheet), System.IO.FileMode.Create)) {

					byte [] buffer = new byte [8192];
					int n = 0;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);

				}
			}

			/* Javascript for persistant style change */
			MakeDir (SubdirPath ("script"));
			using (Stream s = assembly.GetManifestResourceStream (javascript)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("script", javascript), System.IO.FileMode.Create)) {

					byte [] buffer = new byte [8192];
					int n = 0;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);

				}
			}
		}

		public int PageCount {
			get {
				return 	(int) System.Math.Ceiling (Collection.Items.Length / (double)perpage);
			}
		}

		public int TagPageCount (string tag)
		{
			return (int) System.Math.Ceiling (tagSets [tag].Count / (double)perpage);
		}

		public string PhotoThumbPath (int item)
		{
			return System.IO.Path.Combine (requests [2].Name, ImageName (item));
		}

		public string PhotoWebPath (int item)
		{
			return System.IO.Path.Combine (requests [1].Name, ImageName (item));
		}

		public string PhotoOriginalPath (int item)
		{
			return System.IO.Path.Combine (requests [0].Name, ImageName (item));
		}

		public string PhotoIndexPath (int item)
		{
			return (System.IO.Path.GetFileNameWithoutExtension (ImageName (item)) + ".html");
		}

		public static void WritePageNav (System.Web.UI.HtmlTextWriter writer, string id, string url, string name)
		{
			writer.AddAttribute ("id", id);
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", url);
			writer.RenderBeginTag ("a");
			writer.Write (name);
			writer.RenderEndTag ();

			writer.RenderEndTag ();
		}

		public void SavePhotoHtmlIndex (int i)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (PhotoIndexPath (i)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			//writer.Indent = 4;

			//writer.Write ("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");

			WriteHeader (writer);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (GalleryName);
			writer.RenderEndTag ();

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			if (i > 0)
				// Abbreviation of previous
				WritePageNav (writer, "prev", PhotoIndexPath (i - 1), Catalog.GetString("Prev"));

			WritePageNav (writer, "index", IndexPath (i / perpage), Catalog.GetString("Index"));

			if (ExportTags)
				WritePageNav (writer, "tagpage", TagsIndexPath (), Catalog.GetString ("Tags"));

			if (i < Collection.Count -1)
				WritePageNav (writer, "next", PhotoIndexPath (i + 1), Catalog.GetString("Next"));

			writer.RenderEndTag (); //navi

			writer.RenderEndTag (); //header

			writer.AddAttribute ("class", "photo");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", PhotoOriginalPath (i));
			writer.RenderBeginTag ("a");

			writer.AddAttribute ("src", PhotoWebPath (i));
			writer.AddAttribute ("alt", "#");
			writer.AddAttribute ("class", "picture");
			writer.RenderBeginTag ("img");
			writer.RenderEndTag (); //img
			writer.RenderEndTag (); //a

			writer.AddAttribute ("id", "description");
			writer.RenderBeginTag ("div");
			writer.Write (Collection [i].Description);
			writer.RenderEndTag (); //div#description

			writer.RenderEndTag (); //div.photo

			WriteTagsLinks (writer, Collection [i].Tags);

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); // html

			writer.Close ();
			stream.Close ();
		}

		public static string IndexPath (int page_num)
		{
			if (page_num == 0)
				return "index.html";
			else
				return string.Format ("index{0}.html", page_num);
		}

		public static string TagsIndexPath ()
		{
			return "tags.html";
		}

		public static string TagIndexPath (string tag, int page_num)
		{
			string name = "tag_"+tag;
			name = name.Replace ("/", "_").Replace (" ","_");
			if (page_num == 0)
				return name + ".html";
			else
				return name + string.Format ("_{0}.html", page_num);
		}

		static string IndexTitle (int page)
		{
			return string.Format ("{0}", page + 1);
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer)
		{
			WriteHeader (writer, "");
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer, string titleExtension)
		{
			writer.RenderBeginTag ("head");
			/* It seems HtmlTextWriter always uses UTF-8, unless told otherwise */
			writer.Write ("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />");
			writer.WriteLine ();
			writer.RenderBeginTag ("title");
			writer.Write (GalleryName + titleExtension);
			writer.RenderEndTag ();

			writer.Write ("<link type=\"text/css\" rel=\"stylesheet\" href=\"");
			writer.Write (string.Format ("{0}", "style/" + stylesheet));
			writer.Write ("\" title=\"" + dark + "\" media=\"screen\" />" + Environment.NewLine);

			writer.Write ("<link type=\"text/css\" rel=\"prefetch ") ;
			writer.Write ("alternate stylesheet\" href=\"");
			writer.Write (string.Format ("{0}", "style/" + altstylesheet));
			writer.Write ("\" title=\"" + light + "\" media=\"screen\" />" + Environment.NewLine);

			writer.Write ("<script src=\"script/" + javascript + "\"");
			writer.Write (" type=\"text/javascript\"></script>" + Environment.NewLine);

			writer.RenderEndTag ();
		}

		public static void WriteFooter (System.Web.UI.HtmlTextWriter writer)
		{
			writer.AddAttribute ("class", "footer");
			writer.RenderBeginTag ("div");

			writer.Write (Catalog.GetString ("Gallery generated by") + " ");

			writer.AddAttribute ("href", "http://f-spot.org");
			writer.RenderBeginTag ("a");
			writer.Write (string.Format ("{0} {1}", Defines.PACKAGE, Defines.VERSION));
			writer.RenderEndTag ();

			writer.RenderEndTag ();
		}

		public static void WriteStyleSelectionBox (System.Web.UI.HtmlTextWriter writer)
		{
			//Style Selection Box
			writer.AddAttribute ("id", "styleboxcontainer");
			writer.RenderBeginTag ("div");
			writer.AddAttribute ("id", "stylebox");
			writer.AddAttribute ("style", "display: none;");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag ("ul");
			writer.RenderBeginTag ("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", dark);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + dark + "')");
			writer.RenderBeginTag ("a");
			writer.Write (dark);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderBeginTag ("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", light);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + light + "')");
			writer.RenderBeginTag ("a");
			writer.Write (light);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderEndTag (); //ul
			writer.RenderEndTag (); //div stylebox
			writer.RenderBeginTag ("div");
			writer.Write ("<span class=\"style_toggle\">");
			writer.Write ("<a href=\"javascript:toggle_stylebox()\">");
			writer.Write ("<span id=\"showlink\">" + Catalog.GetString("Show Styles") + "</span><span id=\"hidelink\" ");
			writer.Write ("style=\"display:none;\">" + Catalog.GetString("Hide Styles") + "</span></a></span>" + Environment.NewLine);
			writer.RenderEndTag (); //div toggle
			writer.RenderEndTag (); //div styleboxcontainer
		}

		public void WriteTagsLinks (System.Web.UI.HtmlTextWriter writer, Tag[] tags)
		{
			List<Tag> tagsList = new List<Tag> (tags.Length);
			foreach (var tag in tags) {
				tagsList.Add (tag);
			}
			WriteTagsLinks (writer, tagsList);
		}

		public void WriteTagsLinks (System.Web.UI.HtmlTextWriter writer, System.Collections.ICollection tags)
		{

			// check if we should write tags
			if (!ExportTags && tags.Count>0)
				return;

			writer.AddAttribute ("id", "tagbox");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag ("h1");
			writer.Write (Catalog.GetString ("Tags"));
			writer.RenderEndTag (); //h1
			writer.AddAttribute ("id", "innertagbox");
			writer.RenderBeginTag ("ul");
			foreach (Tag tag in tags) {
				writer.AddAttribute ("class", "tag");
				writer.RenderBeginTag ("li");
				writer.AddAttribute ("href", TagIndexPath (tag.Name, 0));
				writer.RenderBeginTag ("a");
				if (ExportTagIcons) {
					writer.AddAttribute ("alt", tag.Name);
					writer.AddAttribute ("longdesc", Catalog.GetString ("Tags: ")+tag.Name);
					writer.AddAttribute ("title", Catalog.GetString ("Tags: ")+tag.Name);
					writer.AddAttribute ("src", TagPath (tag));
					writer.RenderBeginTag ("img");
					writer.RenderEndTag ();
				}
				writer.Write(" ");
				if (ExportTagIcons)
					writer.AddAttribute ("class", "tagtext-icon");
				else
					writer.AddAttribute ("class", "tagtext-noicon");
				writer.RenderBeginTag ("span");
				writer.Write (tag.Name);
				writer.RenderEndTag (); //span.tagtext
				writer.RenderEndTag (); //a href
				writer.RenderEndTag (); //div.tag
			}
			writer.RenderEndTag (); //div#tagbox
		}

		public void SaveTagsPage ()
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (TagsIndexPath ()));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			string titleExtension = " " + Catalog.GetString ("Tags");
			WriteHeader (writer, titleExtension);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.AddAttribute ("id", "tagpage");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (GalleryName + titleExtension);
			writer.RenderEndTag (); //title div

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", IndexPath (0));
			writer.RenderBeginTag ("a");
			writer.Write (Catalog.GetString ("Index"));
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			writer.RenderEndTag (); //navi
			writer.RenderEndTag (); //header

			WriteTagsLinks (writer, allTags.Values);

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

		public void SaveTagIndex (string tag, int page_num)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (TagIndexPath (tag, page_num)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			string titleExtension = ": " + tag;
			WriteHeader (writer, titleExtension);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (GalleryName + titleExtension);
			writer.RenderEndTag (); //title div

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			// link to all photos
			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", IndexPath (0));
			writer.RenderBeginTag ("a");
			writer.Write ("Index");
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			// end link to all photos

			// link to all tags
			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", TagsIndexPath ());
			writer.RenderBeginTag ("a");
			writer.Write ("Tags");
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			// end link to all tags

			writer.AddAttribute ("class", "navilabel");
			writer.RenderBeginTag ("div");
			writer.Write (Catalog.GetString ("Page:"));
			writer.RenderEndTag (); //pages div

			int i;
			for (i = 0; i < TagPageCount (tag); i++) {
				writer.AddAttribute ("class", i == page_num ? "navipage-current" : "navipage");
				writer.RenderBeginTag ("div");

				writer.AddAttribute ("href", TagIndexPath (tag, i));
				writer.RenderBeginTag ("a");
				writer.Write (IndexTitle (i));
				writer.RenderEndTag (); //a

				writer.RenderEndTag (); //navipage
			}
			writer.RenderEndTag (); //navi
			writer.RenderEndTag (); //header

			writer.AddAttribute ("class", "thumbs");
			writer.RenderBeginTag ("div");

			int start = page_num * perpage;
			List<int> tagSet = tagSets [tag];
			int end = Math.Min (start + perpage, tagSet.Count);
			for (i = start; i < end; i++) {
				writer.AddAttribute ("href", PhotoIndexPath ((int) tagSet [i]));
				writer.RenderBeginTag ("a");

				writer.AddAttribute  ("src", PhotoThumbPath ((int) tagSet [i]));
				writer.AddAttribute  ("alt", "#");
				writer.RenderBeginTag ("img");
				writer.RenderEndTag ();

				writer.RenderEndTag (); //a
			}

			writer.RenderEndTag (); //thumbs

			writer.AddAttribute ("id", "gallery_description");
			writer.RenderBeginTag ("div");
			writer.Write (Description);
			writer.RenderEndTag (); //description

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

		public void SaveTagIcons ()
		{
			MakeDir (SubdirPath ("tags"));
			foreach (Tag tag in allTags.Values)
				SaveTagIcon (tag);
		}

		public void SaveTagIcon (Tag tag) {
			Gdk.Pixbuf icon = tag.Icon;
			Gdk.Pixbuf scaled = null;
			if (icon.Height != 52 || icon.Width != 52) {
				scaled=icon.ScaleSimple(52,52,Gdk.InterpType.Bilinear);
			} else
				scaled=icon.Copy ();
			scaled.Save (SubdirPath("tags",TagName(tag)), "png");
			scaled.Dispose ();
		}

		public string TagPath (Tag tag)
		{
			return System.IO.Path.Combine("tags",TagName(tag));
		}

		public string TagName (Tag tag)
		{
			return "tag_"+ ((DbItem)tag).Id+".png";
		}

		public void SaveHtmlIndex (int page_num)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (IndexPath (page_num)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			//writer.Indent = 4;

			//writer.Write ("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			WriteHeader (writer);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");



			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (GalleryName);
			writer.RenderEndTag (); //title div

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			if (ExportTags) {
				// link to all tags
				writer.AddAttribute ("class", "navipage");
				writer.RenderBeginTag ("div");

				writer.AddAttribute ("href", TagsIndexPath ());
				writer.RenderBeginTag ("a");
				writer.Write ("Tags");
				writer.RenderEndTag (); //a

				writer.RenderEndTag (); //navipage
				// end link to all tags
			}

			writer.AddAttribute ("class", "navilabel");
			writer.RenderBeginTag ("div");
			writer.Write (Catalog.GetString ("Page:"));
			writer.RenderEndTag (); //pages div

			int i;
			for (i = 0; i < PageCount; i++) {
				writer.AddAttribute ("class", i == page_num ? "navipage-current" : "navipage");
				writer.RenderBeginTag ("div");

				writer.AddAttribute ("href", IndexPath (i));
				writer.RenderBeginTag ("a");
				writer.Write (IndexTitle (i));
				writer.RenderEndTag (); //a

				writer.RenderEndTag (); //navipage
			}
			writer.RenderEndTag (); //navi
			writer.RenderEndTag (); //header

			writer.AddAttribute ("class", "thumbs");
			writer.RenderBeginTag ("div");

			int start = page_num * perpage;
			int end = Math.Min (start + perpage, Collection.Count);
			for (i = start; i < end; i++) {
				writer.AddAttribute ("href", PhotoIndexPath (i));
				writer.RenderBeginTag ("a");

				writer.AddAttribute  ("src", PhotoThumbPath (i));
				writer.AddAttribute  ("alt", "#");
				writer.RenderBeginTag ("img");
				writer.RenderEndTag ();

				writer.RenderEndTag (); //a
			}

			writer.RenderEndTag (); //thumbs

			writer.AddAttribute ("id", "gallery_description");
			writer.RenderBeginTag ("div");
			writer.Write (Description);
			writer.RenderEndTag (); //description

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

	}
}
