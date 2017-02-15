//
// UriCollection.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007-2009 Stephane Delcroix
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

using System.Collections.Generic;
using System.Xml;

using Hyena;

using GLib;

using FSpot.Core;
using FSpot.Imaging;

namespace FSpot
{
	public class UriCollection : PhotoList
	{
		public UriCollection () : base (new IPhoto [0])
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
			if (App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (uri)) {
				//Console.WriteLine ("using image loader {0}", uri.ToString ());
				Add (new FilePhoto (uri));
			} else {
				var info = FileFactory.NewForUri (uri).QueryInfo ("standard::type,standard::content-type", FileQueryInfoFlags.None, null);

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

		class RssLoader
		{
			public RssLoader (UriCollection collection, SafeUri uri)
			{
				var doc = new XmlDocument ();
				doc.Load (uri.ToString ());
				var ns = new XmlNamespaceManager (doc.NameTable);
				ns.AddNamespace ("media", "http://search.yahoo.com/mrss/");
				ns.AddNamespace ("pheed", "http://www.pheed.com/pheed/");
				ns.AddNamespace ("apple", "http://www.apple.com/ilife/wallpapers");

				List<FilePhoto> items = new List<FilePhoto> ();
				XmlNodeList list = doc.SelectNodes ("/rss/channel/item/media:content", ns);
				foreach (XmlNode item in list) {
					SafeUri image_uri = new SafeUri (item.Attributes ["url"].Value);
					Hyena.Log.DebugFormat ("flickr uri = {0}", image_uri.ToString ());
					items.Add (new FilePhoto (image_uri));
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/pheed:imgsrc", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.DebugFormat ("pheed uri = {0}", uri);
						items.Add (new FilePhoto (image_uri));
					}
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/apple:image", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.DebugFormat ("apple uri = {0}", uri);
						items.Add (new FilePhoto (image_uri));
					}
				}
				collection.Add (items.ToArray ());
			}
		}

		class DirectoryLoader
		{
			readonly UriCollection collection;
			readonly GLib.File file;

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
				List<FilePhoto> items = new List<FilePhoto> ();
				foreach (GLib.FileInfo info in file.EnumerateChildrenFinish (res)) {
					SafeUri i = new SafeUri (file.GetChild (info.Name).Uri);
					Hyena.Log.DebugFormat ("testing uri = {0}", i);
					if (App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (i))
						items.Add (new FilePhoto (i));
				}
				ThreadAssist.ProxyToMain (() => collection.Add (items.ToArray ()));
			}
		}

		protected void LoadItems (System.IO.FileInfo [] files)
		{
			List<IPhoto> items = new List<IPhoto> ();
			foreach (var f in files) {
				if (App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (new SafeUri (f.FullName))) {
					Hyena.Log.Debug (f.FullName);
					items.Add (new FilePhoto (new SafeUri (f.FullName)));
				}
			}

			list = items;
			Reload ();
		}
	}
}
