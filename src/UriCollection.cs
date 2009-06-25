/*
 * UriCollection.cs
 *
 * Author(s):
 *	Larry Ewing  (lewing@novell.com)
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * Copyright (c) 2005-2009 Novell, Inc.
 * Copyright (c) 2007 Stephane Delcroix
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using GLib;

namespace FSpot {
	public class UriCollection : PhotoList {
		public UriCollection () : base (new IBrowsableItem [0])
		{
		}

		public UriCollection (System.IO.FileInfo [] files) : this ()
		{
			LoadItems (files);
		}

		public UriCollection (Uri [] uri) : this ()
		{
			LoadItems (uri);
		}

		public void Add (Uri uri)
		{
			if (FSpot.ImageFile.HasLoader (uri)) {
				//Console.WriteLine ("using image loader {0}", uri.ToString ());
				Add (new FileBrowsableItem (uri));
			} else {
				GLib.FileInfo info = FileFactory.NewForUri (uri).QueryInfo ("standard::type,standard::content-type", FileQueryInfoFlags.None, null);

				if (info.FileType == FileType.Directory)
					new DirectoryLoader (this, uri);
				else {
					// FIXME ugh...
					if (info.ContentType == "text/xml"
					 || info.ContentType == "application/xml"
					 || info.ContentType == "application/rss+xml"
					 || info.ContentType == "text/plain") {
						new RssLoader (this, uri);
					}
				}
			}
		}

		public void LoadItems (Uri [] uris)
		{
			foreach (Uri uri in uris) {
				Add (uri);
			}
		}

		private class RssLoader
		{
			public RssLoader (UriCollection collection, System.Uri uri)
			{
				XmlDocument doc = new XmlDocument ();
				doc.Load (uri.ToString ());
				XmlNamespaceManager ns = new XmlNamespaceManager (doc.NameTable);
				ns.AddNamespace ("media", "http://search.yahoo.com/mrss/");
				ns.AddNamespace ("pheed", "http://www.pheed.com/pheed/");
				ns.AddNamespace ("apple", "http://www.apple.com/ilife/wallpapers");

				ArrayList items = new ArrayList ();
				XmlNodeList list = doc.SelectNodes ("/rss/channel/item/media:content", ns);
				foreach (XmlNode item in list) {
					Uri image_uri = new Uri (item.Attributes ["url"].Value);
					System.Console.WriteLine ("flickr uri = {0}", image_uri.ToString ());
					items.Add (new FileBrowsableItem (image_uri));
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/pheed:imgsrc", ns);
					foreach (XmlNode item in list) {
						Uri image_uri = new Uri (item.InnerText.Trim ());
						System.Console.WriteLine ("pheed uri = {0}", uri);
						items.Add (new FileBrowsableItem (image_uri));
					}
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/apple:image", ns);
					foreach (XmlNode item in list) {
						Uri image_uri = new Uri (item.InnerText.Trim ());
						System.Console.WriteLine ("apple uri = {0}", uri);
						items.Add (new FileBrowsableItem (image_uri));
					}
				}
				collection.Add (items.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem []);
			}
		}

		private class DirectoryLoader
		{
			UriCollection collection;
			Uri uri;
			GLib.File file;

			public DirectoryLoader (UriCollection collection, System.Uri uri)
			{
				this.collection = collection;
				this.uri = uri;
				file = FileFactory.NewForUri (uri);
				file.EnumerateChildrenAsync ("standard::*",
							     FileQueryInfoFlags.None,
							     500,
							     null,
							     InfoLoaded);
										    
			}

			void InfoLoaded (GLib.Object o, GLib.AsyncResult res)
			{
				List<FileBrowsableItem> items = new List<FileBrowsableItem> ();
				foreach (GLib.FileInfo info in file.EnumerateChildrenFinish (res)) {
					Uri i = file.GetChild (info.Name).Uri;
					FSpot.Utils.Log.Debug ("testing uri = {0}", i);
					if (FSpot.ImageFile.HasLoader (i))
						items.Add (new FileBrowsableItem (i));
				}
				Gtk.Application.Invoke (items, System.EventArgs.Empty, delegate (object sender, EventArgs args) {
					collection.Add (items.ToArray ());
				});
			}
		}

		protected void LoadItems (System.IO.FileInfo [] files)
		{
			List<IBrowsableItem> items = new List<IBrowsableItem> ();
			foreach (var f in files) {
				if (FSpot.ImageFile.HasLoader (f.FullName)) {
					Console.WriteLine (f.FullName);
					items.Add (new FileBrowsableItem (f.FullName));
				}
			}

			list = items;
			this.Reload ();
		}
	}


}

