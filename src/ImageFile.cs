using System.IO;

namespace FSpot {
	public class ImageFile {
		protected string path;
		
		protected ImageFile (string path) 
		{
			this.path = path;
		}

		public virtual PixbufOrientation GetOrientation () {
			return PixbufOrientation.TopLeft;
		}
		
		public virtual System.DateTime Date () {
			return File.GetCreationTimeUtc  (this.path);
		}
		
		public static ImageFile Create (string path)
		{
			if (path.ToLower().EndsWith (".jpg") || path.ToLower().EndsWith (".jpeg"))
				return new JpegFile (path);
			else
				return new ImageFile (path);
		}
	} 
}
