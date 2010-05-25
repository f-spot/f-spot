//
// Fspot.Loaders.ImageLoader.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Stephane Delcroix  <sdelcroix@novell.com>
//	Ruben Vermeersch  <ruben@savanne.be>
//
// This is free software. See COPYING for details
//

using FSpot.Utils;
using System;
using System.Collections.Generic;
using Gdk;
using Hyena;

namespace FSpot.Loaders {
	public static class ImageLoader {
		static Dictionary<string, System.Type> name_table;

		static ImageLoader ()
		{
			name_table = new Dictionary<string, System.Type> ();
			System.Type gdk_loader = typeof (GdkImageLoader);
			foreach (string key in ImageFile.NameTable.Keys) {
				name_table [key] = gdk_loader;
			}

			//as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
			//and enable it if needed.
			foreach (Gdk.PixbufFormat format in Gdk.Pixbuf.Formats)
				if (format.Name == "xcf") {
					if (format.IsDisabled)
						format.SetDisabled (false);
					name_table [".xcf"] = typeof (GdkImageLoader);
				}
		}

		public static IImageLoader Create (SafeUri uri)
		{
			string path = uri.AbsolutePath;
			string extension = System.IO.Path.GetExtension (path).ToLower ();
			System.Type t;
			IImageLoader loader;

			if (!name_table.TryGetValue (extension, out t)) {
				GLib.FileInfo info = GLib.FileFactory.NewForUri (uri).QueryInfo ("standard::type,standard::content-type", GLib.FileQueryInfoFlags.None, null);
				if (!name_table.TryGetValue (info.ContentType, out t))
					throw new Exception ("Loader requested for unknown file type: "+extension);
			}

			loader = (IImageLoader) System.Activator.CreateInstance (t);

			return loader;
		}
	}
}
