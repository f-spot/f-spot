using Hyena;

using System;
using System.IO;
using System.Collections.Generic;

using FSpot.Utils;
using Mono.Unix;
using Mono.Unix.Native;
using Gdk;

using GLib;
using TagLib.Image;

using GFileInfo = GLib.FileInfo;

namespace FSpot.Imaging {
	public class ImageFormatException : ApplicationException {
		public ImageFormatException (string msg) : base (msg)
		{
		}
	}

	public static class ImageFile {

#region Factory functionality

		static Dictionary<string, Type> name_table;
		internal static Dictionary<string, Type> NameTable { get { return name_table; } }

        static ImageFile ()
        {
            var base_type = typeof (BaseImageFile);
            var raw_type = typeof (DCRawFile);
            var nef_type = typeof (FSpot.Imaging.NefFile);

            name_table = new Dictionary<string, Type> ();

            // Plain image files
            name_table ["image/gif"] = name_table [".gif"] = base_type;
            name_table ["image/x-pcx"] = name_table [".pcx"] = base_type;
            name_table ["image/x-portable-anymap"] = name_table [".pnm"] = base_type;
            name_table ["image/x-portable-bitmap"] = name_table [".pbm"] = base_type;
            name_table ["image/x-portable-graymap"] = name_table [".pgm"] = base_type;
            name_table ["image/x-portable-pixmap"] = name_table [".ppm"] = base_type;
            name_table ["image/x-bmp"] = name_table ["image/x-MS-bmp"] = name_table [".bmp"] = base_type;
            name_table ["image/jpeg"] = name_table [".jfi"] = name_table [".jfif"] = name_table [".jif"] = name_table [".jpe"] = name_table [".jpeg"] = name_table [".jpg"] = base_type;
            name_table ["image/png"] = name_table [".png"] = base_type;
            name_table ["image/tiff"] = name_table [".tif"] = name_table [".tiff"] = base_type;
            name_table ["image/svg+xml"] = name_table [".svg"] = name_table [".svgz"] = base_type;

            // RAW files
            name_table ["image/arw"] = name_table ["image/x-sony-arw"] = name_table [".arw"] = nef_type;
            name_table ["image/cr2"] = name_table ["image/x-canon-cr2"] = name_table [".cr2"] = typeof (FSpot.Imaging.Cr2File);
            name_table ["image/dng"] = name_table ["image/x-adobe-dng"] = name_table [".dng"] = typeof (FSpot.Imaging.DngFile);
            name_table ["image/nef"] = name_table ["image/x-nikon-nef"] = name_table [".nef"] = nef_type;
            name_table ["image/rw2"] = name_table ["image/x-raw"] = name_table [".rw2"] = raw_type;
            name_table ["image/pef"] = name_table ["image/x-pentax-pef"] = name_table [".pef"] = nef_type;
            name_table ["image/raw"] = name_table ["image/x-panasonic-raw"] = name_table [".raw"] = nef_type;

            // Other types (FIXME: Currently unsupported by Taglib#, this list should shrink).

            name_table [".kdc"] = typeof (FSpot.Imaging.NefFile);
            name_table [".rw2"] = typeof (FSpot.Imaging.DCRawFile);
            name_table [".orf"] =  typeof (FSpot.Imaging.NefFile);
            name_table [".srf"] = typeof (FSpot.Imaging.NefFile);
            name_table [".crw"] = typeof (FSpot.Imaging.Ciff.CiffFile);
            name_table [".mrw"] = typeof (FSpot.Imaging.DCRawFile);
            name_table [".raf"] = typeof (FSpot.Imaging.RafFile);
            name_table [".x3f"] = typeof (FSpot.Imaging.DCRawFile);
            name_table ["image/x-ciff"]  = name_table [".crw"];
            name_table ["image/x-mrw"]   = name_table [".mrw"];
            name_table ["image/x-x3f"]   = name_table [".x3f"];
            name_table ["image/x-orf"]   = name_table [".orf"];
            name_table ["image/x-raf"]   = name_table [".raf"];

            // as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
            // and enable it if needed.
            foreach (Gdk.PixbufFormat format in Gdk.Pixbuf.Formats) {
                if (format.Name == "xcf") {
                    if (format.IsDisabled)
                        format.SetDisabled (false);
                    name_table [".xcf"] = base_type;
                }
            }
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
            if (!file.Exists) {
                return null;
            }

            var extension = uri.GetExtension ().ToLower ();
            if (extension == ".thm") {
                // Ignore video thumbnails.
                return null;
            }

            // Detect mime-type
            var info = file.QueryInfo ("standard::content-type,standard::size", FileQueryInfoFlags.None, null);
            var mime = info.ContentType;
            var size = info.Size;

            if (size == 0) {
                // Empty file
                return null;
            }

            Type t = null;

            if (name_table.TryGetValue (mime, out t)) {
                return t;
            } else if (name_table.TryGetValue (extension, out t)) {
                return t;
            }
            return null;
        }

        public static IImageFile Create (SafeUri uri)
        {
            var t = GetLoaderType (uri);
            if (t == null)
                throw new Exception (String.Format ("Unsupported image: {0}", uri));

            try {
                return (IImageFile) System.Activator.CreateInstance (t, new object[] { uri });
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
			};
			var extension = uri.GetExtension ().ToLower ();
			foreach (string ext in raw_extensions)
				if (ext == extension)
					return true;
			return false;
		}

		public static bool IsJpeg (SafeUri uri)
		{
			string [] jpg_extensions = {".jpg", ".jpeg"};
			var extension = uri.GetExtension ().ToLower ();
			foreach (string ext in jpg_extensions)
				if (ext == extension)
					return true;
			return false;
		}

#endregion
	}

    public interface IImageFile : IDisposable {
        SafeUri Uri { get; }
        Gdk.Pixbuf Load ();
        Cms.Profile GetProfile ();
        Gdk.Pixbuf Load (int max_width, int max_height);
        Stream PixbufStream ();
        ImageOrientation Orientation { get; }
    }

    public class BaseImageFile : IImageFile {
        public SafeUri Uri { get; private set; }
        public ImageOrientation Orientation { get; private set; }

        public BaseImageFile (SafeUri uri)
        {
            Uri = uri;
            Orientation = ImageOrientation.TopLeft;

            using (var metadata_file = Metadata.Parse (uri)) {
                ExtractMetadata (metadata_file);
            }
        }

        protected virtual void ExtractMetadata (TagLib.Image.File metadata) {
            if (metadata != null) {
                Orientation = metadata.ImageTag.Orientation;
            }
        }

		~BaseImageFile ()
		{
			Dispose ();
		}

		public virtual Stream PixbufStream ()
		{
			Hyena.Log.DebugFormat ("open uri = {0}", Uri.ToString ());
			return new GLib.GioStream (GLib.FileFactory.NewForUri (Uri).Read (null));
		}

		protected Gdk.Pixbuf TransformAndDispose (Gdk.Pixbuf orig)
		{
			if (orig == null)
				return null;

			Gdk.Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (orig, this.Orientation);

			orig.Dispose ();

			return rotated;
		}

		public Gdk.Pixbuf Load ()
		{
			using (Stream stream = PixbufStream ()) {
				Gdk.Pixbuf orig = new Gdk.Pixbuf (stream);
				return TransformAndDispose (orig);
			}
		}

		public Gdk.Pixbuf Load (int max_width, int max_height)
		{
			Gdk.Pixbuf full = this.Load ();
			Gdk.Pixbuf scaled  = PixbufUtils.ScaleToMaxSize (full, max_width, max_height);
			full.Dispose ();
			return scaled;
		}

		// FIXME this need to have an intent just like the loading stuff.
		public virtual Cms.Profile GetProfile ()
		{
			return null;
		}

		public void Dispose ()
		{
			Close ();
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Close ()
		{
		}
    }
}
