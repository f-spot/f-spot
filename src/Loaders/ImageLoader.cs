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

namespace FSpot.Loaders {
	public static class ImageLoader {
		static Dictionary<string, System.Type> name_table;

		static ImageLoader ()
		{
			name_table = new Dictionary<string, System.Type> ();
			name_table [".svg"] = typeof (GdkImageLoader);
			name_table [".gif"] = typeof (GdkImageLoader);
			name_table [".bmp"] = typeof (GdkImageLoader);
			name_table [".jpeg"] = typeof (GdkImageLoader);
			name_table [".jpg"] = typeof (GdkImageLoader);
			name_table [".png"] = typeof (GdkImageLoader);
			name_table [".cr2"] = typeof (GdkImageLoader);
			name_table [".nef"] = typeof (GdkImageLoader);
			name_table [".pef"] = typeof (GdkImageLoader);
			name_table [".raw"] = typeof (GdkImageLoader);
			name_table [".kdc"] = typeof (GdkImageLoader);
			name_table [".arw"] = typeof (GdkImageLoader);
			name_table [".tiff"] = typeof (GdkImageLoader);
			name_table [".tif"] = typeof (GdkImageLoader);
			name_table [".orf"] =  typeof (GdkImageLoader);
			name_table [".srf"] = typeof (GdkImageLoader);
			name_table [".dng"] = typeof (GdkImageLoader);
			name_table [".crw"] = typeof (GdkImageLoader);
			name_table [".ppm"] = typeof (GdkImageLoader);
			name_table [".mrw"] = typeof (GdkImageLoader);
			name_table [".pcx"] = typeof (GdkImageLoader);
			name_table [".raf"] = typeof (GdkImageLoader);
			name_table [".x3f"] = typeof (GdkImageLoader);

			//as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
			//and enable it if needed.
			foreach (Gdk.PixbufFormat format in Gdk.Pixbuf.Formats)
				if (format.Name == "xcf") {
					if (format.IsDisabled)
						format.SetDisabled (false);
					name_table [".xcf"] = typeof (GdkImageLoader);
				}
		}

		public static IImageLoader Create (Uri uri)
		{
			string path = uri.AbsolutePath;
			string extension = System.IO.Path.GetExtension (path).ToLower ();
			System.Type t;
			IImageLoader loader;

			if (!name_table.TryGetValue (extension, out t))
				throw new Exception ("Loader requested for unknown file type: "+extension);

			loader = (IImageLoader) System.Activator.CreateInstance (t);

			return loader;
		}
	}
}
