using System.IO;

namespace FSpot {
	public class ImageFile {
		protected string path;

		static System.Collections.Hashtable name_table;

		protected ImageFile (string path) 
		{
			this.path = path;
		}
		
		public virtual System.IO.Stream PixbufStream ()
		{
			return System.IO.File.OpenRead (this.path);
		}

		static ImageFile ()
		{
			name_table = new System.Collections.Hashtable ();
			name_table [".jpeg"] = typeof (JpegFile);
			name_table [".jpg"] = typeof (JpegFile);
			name_table [".png"] = typeof (FSpot.Png.PngFile);
			name_table [".cr2"] = typeof (FSpot.Tiff.Cr2File);
			name_table [".nef"] = typeof (FSpot.Tiff.NefFile);
			name_table [".tiff"] = typeof (FSpot.Tiff.TiffFile);
			name_table [".tif"] = typeof (FSpot.Tiff.TiffFile);
			name_table [".dng"] = typeof (FSpot.Tiff.TiffFile);
			name_table [".crw"] = typeof (FSpot.Ciff.CiffFile);
			name_table [".ppm"] = typeof (FSpot.Pnm.PnmFile);
		}

		public string Path {
			get {
				return this.path;
			}
		}


		public PixbufOrientation Orientation {
			get {
				return GetOrientation ();
			}
		}
		
		public virtual void Save (Gdk.Pixbuf pixbuf, System.IO.Stream stream)
		{
			PixbufUtils.Save (pixbuf, stream, "jpeg", null, null);
		}

		protected Gdk.Pixbuf TransformAndDispose (Gdk.Pixbuf orig)
		{
			if (orig == null)
				return null;

			Gdk.Pixbuf rotated = PixbufUtils.TransformOrientation (orig, this.Orientation, true);
			//ValidateThumbnail (photo, rotated);
			if (rotated != orig)
				orig.Dispose ();
			
			return rotated;
		}

		public virtual Gdk.Pixbuf Load ()
		{
			Gdk.Pixbuf orig = new Gdk.Pixbuf (this.Path);
			return TransformAndDispose (orig);
		}
		
		public virtual Gdk.Pixbuf Load (int max_width, int max_height)
		{
			return PixbufUtils.LoadAtMaxSize (this.Path, max_width, max_height);
		}
		
		public virtual PixbufOrientation GetOrientation () 
		{
			return PixbufOrientation.TopLeft;
		}
		
		public virtual System.DateTime Date () 
		{
			return File.GetCreationTimeUtc  (this.path);
		}
		
		public static ImageFile Create (string path)
		{
			string extension = System.IO.Path.GetExtension (path).ToLower ();
			System.Type t = (System.Type) name_table [extension];
			
			ImageFile img;
			if (t != null)
				img = (ImageFile) System.Activator.CreateInstance (t, new object[] {path});
			else 
				img = new ImageFile (path);

			return img;
		}
	} 
}
