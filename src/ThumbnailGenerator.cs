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
		
		public static void Save (Gdk.Pixbuf image, string path)
		{			
			string uri = UriList.PathToFileUri (path).ToString ();
			System.DateTime mtime = System.IO.File.GetLastWriteTime (path);
			
			PixbufUtils.SetOption (image, "tEXt::Thumb::URI", uri);
			PixbufUtils.SetOption (image, "tEXt::Thumb::MTime", 
					       ((uint)GLib.Marshaller.DateTimeTotime_t (mtime)).ToString ());
			
			System.Console.WriteLine ("saving uri \"{0}\" mtime \"{1}\"", 
						  image.GetOption ("tEXt::Thumb::URI"), 
						  image.GetOption ("tEXt::Thumb::MTime"));
			
			string large_path = Gnome.Thumbnail.PathForUri (uri, Gnome.ThumbnailSize.Large);
			try {
				ThumbnailCache.Default.RemoveThumbnailForPath (large_path);
			} finally {
				factory.SaveThumbnail (image, uri, mtime);
			}
		}

		protected override void ProcessRequest (RequestItem request)
		{
			// Load the image.
			base.ProcessRequest (request);

			System.Console.WriteLine ("Got here");
			Gdk.Pixbuf image = request.result;
			if (image != null) {
				Save (image, request.path);
				image.Dispose ();
			}
		}
	}
}
