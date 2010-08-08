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

using Hyena;
using GLib;

using FSpot.Core;
using FSpot.Imaging;

namespace FSpot {
	public class UriCollection : PhotoList {
		public UriCollection () : base (new IBrowsableItem [0])
		{
		}

		public UriCollection (System.IO.FileInfo [] files) : this ()
		{
			LoadItems (files);
		}

		public UriCollection (SafeUri [] uri) : this ()
		{
			LoadItems (uri);
		}

		public void Add (SafeUri uri)
		{
			if (ImageFile.HasLoader (uri)) {
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

		public void LoadItems (SafeUri [] uris)
		{
			foreach (SafeUri uri in uris) {
				Add (uri);
			}
		}

		private class RssLoader
		{
			public RssLoader (UriCollection collection, SafeUri uri)
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
					SafeUri image_uri = new SafeUri (item.Attributes ["url"].Value);
					Hyena.Log.DebugFormat ("flickr uri = {0}", image_uri.ToString ());
					items.Add (new FileBrowsableItem (image_uri));
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/pheed:imgsrc", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.DebugFormat ("pheed uri = {0}", uri);
						items.Add (new FileBrowsableItem (image_uri));
					}
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/apple:image", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.DebugFormat ("apple uri = {0}", uri);
						items.Add (new FileBrowsableItem (image_uri));
					}
				}
				collection.Add (items.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem []);
			}
		}

		private class DirectoryLoader
		{
			UriCollection collection;
			GLib.File file;

			public DirectoryLoader (UriCollection collection, SafeUri uri)
			{
				this.collection = collection;
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
					SafeUri i = new SafeUri (file.GetChild (info.Name).Uri);
					Hyena.Log.DebugFormat ("testing uri = {0}", i);
					if (ImageFile.HasLoader (i))
						items.Add (new FileBrowsableItem (i));
				}
				ThreadAssist.ProxyToMain (() => {
					collection.Add (items.ToArray ());
				});
			}
		}

		protected void LoadItems (System.IO.FileInfo [] files)
		{
			List<IBrowsableItem> items = new List<IBrowsableItem> ();
			foreach (var f in files) {
				if (ImageFile.HasLoader (new SafeUri (f.FullName))) {
					Hyena.Log.Debug (f.FullName);
					items.Add (new FileBrowsableItem (new SafeUri (f.FullName)));
				}
			}

			list = items;
			this.Reload ();
		}
	}


}
