namespace FSpot {
	public class ThumbnailGenerator : PixbufLoader {

		private Gnome.ThumbnailFactory large_factory;
		private Gnome.ThumbnailFactory small_factory;

		static public ThumbnailGenerator Default = new ThumbnailGenerator ();

		public ThumbnailGenerator ()
		{
			large_factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
			small_factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Normal);
		}
		
		protected override void ProcessRequest (RequestItem request)
		{
			// Load the image.
			base.ProcessRequest (request);

			System.Console.WriteLine ("Got here");
			Gdk.Pixbuf image = request.result;
			if (image != null) {
				string path = request.path;
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
					
					System.IO.File.Delete (large_path);
					ThumbnailCache.Default.RemoveThumbnailForPath (large_path);
				} finally {
					large_factory.SaveThumbnail (image, uri, mtime);
				}
#if SAVE_NORMAL_THUMBS
				Gdk.Pixbuf small = PixbufUtils.ScaleToMaxSize (image, 128, 128);
				PixbufUtils.CopyThumbnailOptions (image, small);
				string small_path = Gnome.Thumbnail.PathForUri (uri, Gnome.ThumbnailSize.Normal);
				try {
					System.IO.File.Delete (small_path);
					ThumbnailCache.Default.RemoveThumbnailForPath (small_path);
				} finally {
					small_factory.SaveThumbnail (small, uri, mtime);
				}
				small.Dispose ();
#endif			
				image.Dispose ();
			}
		}
	}
}
