/*
 * UriCollection.cs
 *
 * Author(s):
 *	Larry Ewing  (lewing@novell.com)
 *	Stephane Delcroix  (stephane@delcroix.org)
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace FSpot {
	public class UriCollection : PhotoList {
		public UriCollection () : base (new IBrowsableItem [0])
		{
		}

		public UriCollection (FileInfo [] files) : this ()
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
				Gnome.Vfs.FileInfo info = new Gnome.Vfs.FileInfo (uri.ToString (),
						Gnome.Vfs.FileInfoOptions.GetMimeType);


				//Console.WriteLine ("url {0} MimeType {1}", uri, info.MimeType);

				if (info.Type == Gnome.Vfs.FileType.Directory)
					new DirectoryLoader (this, uri);
				else {
					// FIXME ugh...
					if (info.MimeType == "text/xml"
						|| info.MimeType == "application/xml"
					|| info.MimeType == "application/rss+xml"
					|| info.MimeType == "text/plain") {
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

			public DirectoryLoader (UriCollection collection, System.Uri uri)
			{
				this.collection = collection;
				this.uri = uri;
				Gnome.Vfs.Directory.GetEntries (uri.ToString (),
						Gnome.Vfs.FileInfoOptions.Default,
						20,
						(int)Gnome.Vfs.Async.Priority.Default,
						InfoLoaded);
			}

			private void InfoLoaded (Gnome.Vfs.Result result, Gnome.Vfs.FileInfo []info, uint entries_read)
			{
				if (result != Gnome.Vfs.Result.Ok && result != Gnome.Vfs.Result.ErrorEof)
					return;

				ArrayList items = new ArrayList ();

				for (int i = 0; i < entries_read; i++) {
					Gnome.Vfs.Uri vfs = new Gnome.Vfs.Uri (uri.ToString ());
					vfs = vfs.AppendFileName (info [i].Name);
					Uri file = new Uri (vfs.ToString ());
					System.Console.WriteLine ("tesing uri = {0}", file.ToString ());

					if (FSpot.ImageFile.HasLoader (file))
						items.Add (new FileBrowsableItem (file));
				}

				Gtk.Application.Invoke (items, System.EventArgs.Empty, delegate (object sender, EventArgs args) {
					collection.Add (items.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem []);
				});
			}
		}

		protected void LoadItems (FileInfo [] files)
		{
			List<IBrowsableItem> items = new List<IBrowsableItem> ();
			foreach (FileInfo f in files) {
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

