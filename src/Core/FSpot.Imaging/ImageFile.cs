//
// ImageFile.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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
using System.Linq;

using Hyena;

using Gdk;

using GLib;

using GFileInfo = GLib.FileInfo;
using FSpot.Utils;

namespace FSpot.Imaging
{
	public static class ImageFile
	{
		#region Factory functionality
		public static Dictionary<string, Type> NameTable { get; private set; }

		static ImageFile ()
		{
			var base_type = typeof(BaseImageFile);
			var raw_type = typeof(DCRawImageFile);
			var nef_type = typeof(NefImageFile);
			var cr2_type = typeof(Cr2ImageFile);
			var dng_type = typeof(DngImageFile);
			var ciff_type = typeof(Ciff.CiffImageFile);
			var raf_type = typeof(RafImageFile);

			NameTable = new Dictionary<string, Type> ();

			// Plain image files
			NameTable ["image/gif"] = NameTable [".gif"] = base_type;
			NameTable ["image/x-pcx"] = NameTable [".pcx"] = base_type;
			NameTable ["image/x-portable-anymap"] = NameTable [".pnm"] = base_type;
			NameTable ["image/x-portable-bitmap"] = NameTable [".pbm"] = base_type;
			NameTable ["image/x-portable-graymap"] = NameTable [".pgm"] = base_type;
			NameTable ["image/x-portable-pixmap"] = NameTable [".ppm"] = base_type;
			NameTable ["image/x-bmp"] = NameTable ["image/x-MS-bmp"] = NameTable [".bmp"] = base_type;
			NameTable ["image/jpeg"] = NameTable [".jfi"] = NameTable [".jfif"] = NameTable [".jif"] = NameTable [".jpe"] = NameTable [".jpeg"] = NameTable [".jpg"] = base_type;
			NameTable ["image/png"] = NameTable [".png"] = base_type;
			NameTable ["image/tiff"] = NameTable [".tif"] = NameTable [".tiff"] = base_type;
			NameTable ["image/svg+xml"] = NameTable [".svg"] = NameTable [".svgz"] = base_type;

			// RAW files
			NameTable ["image/arw"] = NameTable ["image/x-sony-arw"] = NameTable [".arw"] = nef_type;
			NameTable ["image/cr2"] = NameTable ["image/x-canon-cr2"] = NameTable [".cr2"] = cr2_type;
			NameTable ["image/dng"] = NameTable ["image/x-adobe-dng"] = NameTable [".dng"] = dng_type;
			NameTable ["image/nef"] = NameTable ["image/x-nikon-nef"] = NameTable [".nef"] = nef_type;
			NameTable ["image/rw2"] = NameTable ["image/x-raw"] = NameTable [".rw2"] = raw_type;
			NameTable ["image/pef"] = NameTable ["image/x-pentax-pef"] = NameTable [".pef"] = nef_type;
			NameTable ["image/raw"] = NameTable ["image/x-panasonic-raw"] = NameTable [".raw"] = nef_type;

			// Other types (FIXME: Currently unsupported by Taglib#, this list should shrink).

			NameTable ["image/x-ciff"] = NameTable [".crw"] = ciff_type;
			NameTable ["image/x-mrw"] = NameTable [".mrw"] = raw_type;
			NameTable ["image/x-x3f"] = NameTable [".x3f"] = raw_type;
			NameTable ["image/x-orf"] = NameTable [".orf"] = nef_type;
			NameTable ["image/x-raf"] = NameTable [".raf"] = raf_type;
			NameTable [".kdc"] = nef_type;
			NameTable [".rw2"] = raw_type;
			NameTable [".srf"] = nef_type;
			NameTable [".srw"] = raw_type; // Samsung NX Raw files, supported by latest dcraw


			// as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
			// and enable it if needed.
			foreach (Gdk.PixbufFormat format in Gdk.Pixbuf.Formats) {
				if (format.Name == "xcf") {
					if (format.IsDisabled)
						format.SetDisabled (false);
					NameTable [".xcf"] = base_type;
				}
			}
		}

		public static List<string> UnitTestImageFileTypes ()
		{
			return NameTable.Keys.ToList();
		}

		public static bool HasLoader (SafeUri uri)
		{
			return GetLoaderType (uri) != null;
		}

		static Type GetLoaderType (SafeUri uri)
		{
			// check if GIO can find the file, which is not the case
			// with filenames with invalid encoding
			var file = GLib.FileFactory.NewForUri (uri);
			if (!file.Exists)
             		   return null;

			string extension = uri.GetExtension ().ToLower ();

			// Ignore video thumbnails
			if (extension == ".thm")
		                return null;

			// Detect mime-type
			var info = file.QueryInfo ("standard::content-type,standard::size", FileQueryInfoFlags.None, null);
			string mime = info.ContentType;
			long size = info.Size;

			// Empty file
			if (size == 0)
		                return null;

			Type t = null;

			if (NameTable.TryGetValue (mime, out t))
				return t;

			if (NameTable.TryGetValue (extension, out t))
				return t;

			return null;
		}

		public static IImageFile Create (SafeUri uri)
		{
			var t = GetLoaderType (uri);
			if (t == null)
				throw new Exception (String.Format ("Unsupported image: {0}", uri));

			try {
				return (IImageFile)System.Activator.CreateInstance (t, new object[] { uri });
			} catch (Exception e) {
				Hyena.Log.DebugException (e);
				throw e;
			}
		}

		public static bool IsRaw (SafeUri uri)
		{
			string [] raw_extensions = {
				".arw",
				".crw",
				".cr2",
				".dng",
				".mrw",
				".nef",
				".orf",
				".pef",
				".raw",
				".raf",
				".rw2",
				".srw",
			};
			var extension = uri.GetExtension ().ToLower ();
			return raw_extensions.Any (x => x == extension);
		}

		public static bool IsJpeg (SafeUri uri)
		{
			string [] jpg_extensions = {".jpg", ".jpeg", ".jpe", ".jfi", ".jfif", ".jif"};
			var extension = uri.GetExtension ().ToLower ();
			return jpg_extensions.Any (x => x == extension);
		}
		#endregion
	}
}
