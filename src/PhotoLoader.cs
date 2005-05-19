namespace FSpot {
	public class PhotoLoader {
		public PhotoQuery query;
		static int THUMBNAIL_SIZE = 256;

		public Gdk.Pixbuf Load (int index) {
			return Load (query, index);
		}

		static public Gdk.Pixbuf Load (PhotoQuery query, int index)
		{
			Photo photo = query.Photos [index];
			return Load (photo);
		}

		static public Gdk.Pixbuf Load (Photo photo) 
		{
			ImageFile img = ImageFile.Create (photo.DefaultVersionPath);
			Gdk.Pixbuf pixbuf = img.Load ();
			ValidateThumbnail (photo, pixbuf);
			return pixbuf;
		}

		static public Gdk.Pixbuf LoadAtMaxSize (Photo photo, int width, int height) 
		{
			ImageFile img = ImageFile.Create (photo.DefaultVersionPath);
			Gdk.Pixbuf pixbuf = img.Load (width, height);
			ValidateThumbnail (photo.DefaultVersionPath, pixbuf);
			return pixbuf;
		}

		static public Gdk.Pixbuf ValidateThumbnail (Photo photo, Gdk.Pixbuf pixbuf)
		{
			return ValidateThumbnail (photo.DefaultVersionPath, pixbuf);
		}

		static public bool ThumbnailIsValid (System.Uri uri, Gdk.Pixbuf thumbnail)
		{
			return FSpot.ThumbnailGenerator.ThumbnailIsValid (thumbnail, uri);
		}

		static public Gdk.Pixbuf ValidateThumbnail (string photo_path, Gdk.Pixbuf pixbuf)
		{			
			System.Uri uri = UriList.PathToFileUri (photo_path);
			string thumbnail_path = Gnome.Thumbnail.PathForUri (uri.ToString (), 
									    Gnome.ThumbnailSize.Large);

			Gdk.Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

			if (pixbuf != null && thumbnail != null) {
				if (!ThumbnailIsValid (uri, thumbnail)) {
					System.Console.WriteLine ("regnerating thumbnail");
					FSpot.ThumbnailGenerator.Default.Request (photo_path, 0, 256, 256);
				}

			}

			if (thumbnail != null)
				thumbnail.Dispose ();
			
			return pixbuf;
		}

		public PhotoLoader (PhotoQuery query)
		{
			this.query = query;
		}
	}
}
