//
// ImageLoader.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
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

using FSpot.Imaging;

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
