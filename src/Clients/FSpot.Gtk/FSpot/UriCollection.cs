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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Hyena;

using FSpot.Core;
using FSpot.Imaging;
using FSpot.FileSystem;

namespace FSpot
{
	public class UriCollection : PhotoList
	{
		public UriCollection () : base (Array.Empty<IPhoto> ())
		{
		}

		public UriCollection (FileInfo [] files) : this ()
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
				var attrs = File.GetAttributes (uri.AbsolutePath);
				if (attrs.HasFlag (FileAttributes.Directory))
					new DirectoryLoader (this, uri);
				else {
					// FIXME ugh...
					var contentType = new DotNetFile ().GetMimeType (uri);
					if (contentType == "text/xml"
					 || contentType == "application/xml"
					 || contentType == "application/rss+xml"
					 || contentType == "text/plain") {
						new RssLoader (this, uri);
					}
				}
			}
		}

		public void LoadItems (SafeUri [] uris)
		{
			if (uris == null || uris.Length == 0)
				return;
			
			foreach (var uri in uris)
				Add (uri);
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

				var items = new List<FilePhoto> ();
				XmlNodeList list = doc.SelectNodes ("/rss/channel/item/media:content", ns);
				foreach (XmlNode item in list) {
					SafeUri image_uri = new SafeUri (item.Attributes ["url"].Value);
					Hyena.Log.Debug ($"flickr uri = {image_uri.ToString ()}");
					items.Add (new FilePhoto (image_uri));
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/pheed:imgsrc", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.Debug ($"pheed uri = {uri}");
						items.Add (new FilePhoto (image_uri));
					}
				}

				if (list.Count < 1) {
					list = doc.SelectNodes ("/rss/channel/item/apple:image", ns);
					foreach (XmlNode item in list) {
						SafeUri image_uri = new SafeUri (item.InnerText.Trim ());
						Hyena.Log.Debug ($"apple uri = {uri}");
						items.Add (new FilePhoto (image_uri));
					}
				}
				collection.Add (items.ToArray ());
			}
		}

		// FIXME, Getting rid of GLib/gio for now
		class DirectoryLoader
		{
			readonly UriCollection collection;
			//readonly GLib.File file;

			public DirectoryLoader (UriCollection collection, SafeUri uri)
			{
				this.collection = collection;
				//file = FileFactory.NewForUri (uri);
				//file.EnumerateChildrenAsync ("standard::*", FileQueryInfoFlags.None, 500, null, InfoLoaded);
				//var dir = new DirectoryInfo (uri.AbsolutePath);
			}

			//void InfoLoaded (GLib.Object o, GLib.AsyncResult res)
			//{
			//	var items = new List<FilePhoto> ();

			//	foreach (GLib.FileInfo info in file.EnumerateChildrenFinish (res)) {
			//		var i = new SafeUri (file.GetChild (info.Name).Uri);
			//		Log.Debug ($"testing uri = {i}");
			//		if (App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (i))
			//			items.Add (new FilePhoto (i));
			//	}

			//	ThreadAssist.ProxyToMain (() => collection.Add (items.ToArray ()));
			//}
		}

		protected void LoadItems (FileInfo [] files)
		{
			var items = new List<IPhoto> ();
			foreach (var f in files) {
				if (App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (new SafeUri (f.FullName))) {
					Log.Debug (f.FullName);
					items.Add (new FilePhoto (new SafeUri (f.FullName)));
				}
			}

			list = items;
			Reload ();
		}
	}
}
