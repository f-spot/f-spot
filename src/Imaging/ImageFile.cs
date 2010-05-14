using System;
using System.IO;
using System.Collections;

using FSpot.Utils;
using Mono.Unix;
using Mono.Unix.Native;
using Gdk;

using GFileInfo = GLib.FileInfo;

namespace FSpot {
	public class ImageFormatException : ApplicationException {
		public ImageFormatException (string msg) : base (msg)
		{
		}
	}

	public class ImageFile : IDisposable {
		protected Uri uri;

		static Hashtable name_table;
		internal static Hashtable NameTable { get { return name_table; } }

		public ImageFile (string path) 
		{
			this.uri = UriUtils.PathToFileUri (path);
		}
		
		public ImageFile (Uri uri)
		{
			this.uri = uri;
		}
		
		protected Stream Open ()
		{
			Log.Debug ("open uri = {0}", uri.ToString ());
//			if (uri.IsFile)
//				return new FileStream (uri.LocalPath, FileMode.Open);
			return new GLib.GioStream (GLib.FileFactory.NewForUri (uri).Read (null));
		}

		public virtual Stream PixbufStream ()
		{
			return Open ();
		}

		static ImageFile ()
		{
			name_table = new Hashtable ();
			name_table [".svg"] = typeof (FSpot.Svg.SvgFile);
			name_table [".gif"] = typeof (ImageFile);
			name_table [".bmp"] = typeof (ImageFile);
			name_table [".pcx"] = typeof (ImageFile);
			name_table [".jpeg"] = typeof (JpegFile);
			name_table [".jpg"] = typeof (JpegFile);
			name_table [".png"] = typeof (FSpot.Png.PngFile);
			name_table [".cr2"] = typeof (FSpot.Tiff.Cr2File);
			name_table [".nef"] = typeof (FSpot.Tiff.NefFile);
			name_table [".pef"] = typeof (FSpot.Tiff.NefFile);
			name_table [".raw"] = typeof (FSpot.Tiff.NefFile);
			name_table [".kdc"] = typeof (FSpot.Tiff.NefFile);
			name_table [".arw"] = typeof (FSpot.Tiff.NefFile);
			name_table [".rw2"] = typeof (FSpot.DCRawFile);
			name_table [".tiff"] = typeof (FSpot.Tiff.TiffFile);
			name_table [".tif"] = typeof (FSpot.Tiff.TiffFile);
			name_table [".orf"] =  typeof (FSpot.Tiff.NefFile);
			name_table [".srf"] = typeof (FSpot.Tiff.NefFile);
			name_table [".dng"] = typeof (FSpot.Tiff.DngFile);
			name_table [".crw"] = typeof (FSpot.Ciff.CiffFile);
			name_table [".ppm"] = typeof (FSpot.Pnm.PnmFile);
			name_table [".mrw"] = typeof (FSpot.Mrw.MrwFile);
			name_table [".raf"] = typeof (FSpot.Raf.RafFile);
			name_table [".x3f"] = typeof (FSpot.X3f.X3fFile);

			// add mimetypes for fallback
			name_table ["image/bmp"]     = name_table ["image/x-bmp"] = name_table [".bmp"];
			name_table ["image/gif"]     = name_table [".gif"];
			name_table ["image/pjpeg"]   = name_table ["image/jpeg"] = name_table ["image/jpg"] = name_table [".jpg"];
			name_table ["image/x-png"]   = name_table ["image/png"]  = name_table [".png"];
			name_table ["image/svg+xml"] = name_table [".svg"];
			name_table ["image/tiff"]    = name_table [".tiff"];
			name_table ["image/x-dcraw"] = name_table [".raw"];
			name_table ["image/x-ciff"]  = name_table [".crw"];
			name_table ["image/x-mrw"]   = name_table [".mrw"];
			name_table ["image/x-x3f"]   = name_table [".x3f"];
			name_table ["image/x-orf"]   = name_table [".orf"];
			name_table ["image/x-nef"]   = name_table [".nef"];
			name_table ["image/x-cr2"]   = name_table [".cr2"];
			name_table ["image/x-raf"]   = name_table [".raf"];

			//as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
			//and enable it if needed.
			foreach (Gdk.PixbufFormat format in Gdk.Pixbuf.Formats)
				if (format.Name == "xcf") {
					if (format.IsDisabled)
						format.SetDisabled (false);
					name_table [".xcf"] = typeof (ImageFile);
				}
		}

		public Uri Uri {
			get { return this.uri; }
		}

		public PixbufOrientation Orientation {
			get { return GetOrientation (); }
		}

		public virtual string Description
		{
			get { return null; }
		}
		
		public virtual void Save (Gdk.Pixbuf pixbuf, System.IO.Stream stream)
		{
			throw new NotImplementedException (Catalog.GetString ("Writing to this file format is not supported"));
		}

		protected Gdk.Pixbuf TransformAndDispose (Gdk.Pixbuf orig)
		{
			if (orig == null)
				return null;

			Gdk.Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (orig, this.Orientation);

			orig.Dispose ();
			
			return rotated;
		}
		
		[Obsolete ("Use an Async way to load the pixbuf")]
		public virtual Gdk.Pixbuf Load ()
		{
			using (Stream stream = PixbufStream ()) {
				Gdk.Pixbuf orig = new Gdk.Pixbuf (stream);
				return TransformAndDispose (orig);
			}
		}
		
		[Obsolete ("Use an Async way to load the pixbuf")]
		public virtual Gdk.Pixbuf Load (int max_width, int max_height)
		{
			System.IO.Stream stream = PixbufStream ();
			if (stream == null) {
				Gdk.Pixbuf orig = this.Load ();
				Gdk.Pixbuf scaled = PixbufUtils.ScaleToMaxSize (orig,  max_width, max_height, false);
				orig.Dispose ();
				return scaled;
			}

			using (stream) {
				PixbufUtils.AspectLoader aspect = new PixbufUtils.AspectLoader (max_width, max_height);
				return aspect.Load (stream, Orientation);
			}	
		}
	
		public virtual PixbufOrientation GetOrientation () 
		{
			return PixbufOrientation.TopLeft;
		}
		
		// FIXME this need to have an intent just like the loading stuff.
		public virtual Cms.Profile GetProfile ()
		{
			return null;
		}
		
		public virtual System.DateTime Date 
		{
			get {
				// FIXME mono uses the file change time (ctime) incorrectly
				// as the creation time so we try to work around that slightly
				GFileInfo info = GLib.FileFactory.NewForUri (uri).QueryInfo ("time::modified,time::created", GLib.FileQueryInfoFlags.None, null);
				DateTime write = NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::modified"));
				DateTime create = NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::created"));

				if (create < write)
					return create;
				else 
					return write;
			}
		}

		[Obsolete ("use HasLoader (System.Uri) instead")]
		public static bool HasLoader (string path)
		{
			return HasLoader (UriUtils.PathToFileUri (path));
		}
		
		public static bool HasLoader (Uri uri)
		{
			return GetLoaderType (uri) != null;
		}

		static Type GetLoaderType (Uri uri)
		{
			string path = uri.AbsolutePath;
			string extension = System.IO.Path.GetExtension (path).ToLower ();
			Type t = (Type) name_table [extension];

			if (t == null) {
				GLib.FileInfo info = GLib.FileFactory.NewForUri (uri).QueryInfo ("standard::type,standard::content-type", GLib.FileQueryInfoFlags.None, null);
				t = (Type) name_table [info.ContentType];
			}

			return t;
		}
		
		[Obsolete ("use Create (System.Uri) instead")]
		public static ImageFile Create (string path)
		{
			return Create (UriUtils.PathToFileUri (path));
		}

		public static ImageFile Create (Uri uri)
		{
			System.Type t = GetLoaderType (uri);
			ImageFile img;

			if (t != null)
				img = (ImageFile) System.Activator.CreateInstance (t, new object[] { uri });
			else 
				img = new ImageFile (uri);

			return img;
		}
		
		// FIXME these are horrible hacks to get a temporary name
		// with the right extension for ImageFile until we use the mime data
		// properly.  It is here to make sure we can find the places that use
		// this hack
		public static string TempPath (string name)
		{
			return TempPath (name, System.IO.Path.GetExtension (name));
		}
		
		public static string TempPath (string name, string extension)
		{
			string temp = System.IO.Path.GetTempFileName ();
			string imgtemp = temp + "." + extension;

			System.IO.File.Move (temp, imgtemp);

			return imgtemp;
		}

		public void Dispose ()
		{
			Close ();
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Close ()
		{
		}

		public static bool IsRaw (string name)
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
			foreach (string ext in raw_extensions)
				if (ext == System.IO.Path.GetExtension (name).ToLower ())
					return true;
			return false;
		}

		public static bool IsJpeg (string name)
		{
			string [] jpg_extensions = {".jpg", ".jpeg"};
			foreach (string ext in jpg_extensions)
				if (ext == System.IO.Path.GetExtension (name).ToLower ())
					return true;
			return false;
		}
	} 
}
