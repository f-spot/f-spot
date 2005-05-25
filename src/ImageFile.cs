using System.IO;

namespace FSpot {
	public class ImageFile {
		protected string path;

		protected ImageFile (string path) 
		{
			this.path = path;
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
			switch (System.IO.Path.GetExtension (path).ToLower ()) {
			case ".jpeg":
				return new JpegFile (path);
			case ".jpg":
				return new JpegFile (path);
			case ".png":
				return new FSpot.Png.PngFile (path);
			case ".cr2":
				return new FSpot.Tiff.Cr2File (path);
			case ".nef":
				return new FSpot.Tiff.NefFile (path);
			case ".tiff":
			case ".tif":
			case ".dng":
				//case ".orf":
				return new FSpot.Tiff.TiffFile (path);
			case ".crw":
				return new FSpot.Ciff.CiffFile (path);
			case ".ppm":
				return new FSpot.Pnm.PnmFile (path);
			default:
				return new ImageFile (path);
			}
		}
	} 
}
