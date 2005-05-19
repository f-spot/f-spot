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

		public virtual Gdk.Pixbuf Load ()
		{
			Gdk.Pixbuf orig = new Gdk.Pixbuf (this.Path);
			
			Gdk.Pixbuf rotated = PixbufUtils.TransformOrientation (orig, this.Orientation, true);
			//ValidateThumbnail (photo, rotated);
			if (rotated != orig)
				orig.Dispose ();
			
			return rotated;
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
			if (path.ToLower().EndsWith (".jpg") || path.ToLower().EndsWith (".jpeg"))
				return new JpegFile (path);
			else if (path.ToLower ().EndsWith (".crw"))
				return new FSpot.Ciff.CiffFile (path);
			else
				return new ImageFile (path);
		}
	} 
}
