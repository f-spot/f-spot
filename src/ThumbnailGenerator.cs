namespace FSpot {
	public class ThumbnailGenerator : PixbufLoader {
		private static Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
		static public ThumbnailGenerator Default = new ThumbnailGenerator ();

		public static Gdk.Pixbuf Create (string path)
		{
			try {
				Gdk.Pixbuf image = PixbufUtils.LoadAtMaxSize (path, 256, 265);
				Save (image, path);
				return image;
			} catch {
				return null;
			}
		}
		

		int block_count;
		public void Block ()
		{

		}

		public static bool ThumbnailIsValid (Gdk.Pixbuf thumbnail, System.Uri uri)
		{
			bool valid = false;

			try {			
				System.DateTime mtime = System.IO.File.GetLastWriteTime (uri.LocalPath);
				valid  = Gnome.Thumbnail.IsValid (thumbnail, uri.ToString (), mtime);
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				valid = false;
			}
			
			return valid;
		}

		public static string ThumbnailPath (System.Uri uri)
		{
			string large_path = Gnome.Thumbnail.PathForUri (uri.ToString (), Gnome.ThumbnailSize.Large);
			return large_path;
		}

		public static string ThumbnailPath (string path) 
		{
			return ThumbnailPath (UriList.PathToFileUri (path));
		}

		public static void Save (Gdk.Pixbuf image, string path)
		{			
			string uri = UriList.PathToFileUri (path).ToString ();
			System.DateTime mtime = System.IO.File.GetLastWriteTime (path);
			
			PixbufUtils.SetOption (image, "tEXt::Thumb::URI", uri);
			PixbufUtils.SetOption (image, "tEXt::Thumb::MTime", 
					       ((uint)GLib.Marshaller.DateTimeTotime_t (mtime)).ToString ());

			//System.Console.WriteLine ("saving uri \"{0}\" mtime \"{1}\"", 
			//			  image.GetOption ("tEXt::Thumb::URI"), 
			//			  image.GetOption ("tEXt::Thumb::MTime"));
			
			string large_path = ThumbnailPath (uri);
			try {
				ThumbnailCache.Default.RemoveThumbnailForPath (large_path);
			} finally {
				factory.SaveThumbnail (image, uri, mtime);
			}
		}

		protected override void EmitLoaded (System.Collections.Queue results)
		{
			base.EmitLoaded (results);
			
			foreach (RequestItem r in results) {
				if (r.result != null)
					r.result.Dispose ();
			}
				
		}

		protected override void ProcessRequest (RequestItem request)
		{
			base.ProcessRequest (request);

			Gdk.Pixbuf image = request.result;
			if (image != null) {
				Save (image, request.path);
			}
		}
	}
}
