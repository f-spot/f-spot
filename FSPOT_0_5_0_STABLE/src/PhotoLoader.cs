using FSpot.Utils;

namespace FSpot {
	public class PhotoLoader {
		public PhotoQuery query;

		public Gdk.Pixbuf Load (int index) {
			return Load (query, index);
		}

		static public Gdk.Pixbuf Load (IBrowsableCollection collection, int index)
		{
			IBrowsableItem item = collection [index];
			return Load (item);
		}

		static public Gdk.Pixbuf Load (IBrowsableItem item) 
		{
			using (ImageFile img = ImageFile.Create (item.DefaultVersionUri)) {
				Gdk.Pixbuf pixbuf = img.Load ();
				ValidateThumbnail (item, pixbuf);
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf LoadAtMaxSize (IBrowsableItem item, int width, int height) 
		{
			using (ImageFile img = ImageFile.Create (item.DefaultVersionUri)) {
				Gdk.Pixbuf pixbuf = img.Load (width, height);
				ValidateThumbnail (item.DefaultVersionUri, pixbuf);
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf ValidateThumbnail (IBrowsableItem item, Gdk.Pixbuf pixbuf)
		{
			return ValidateThumbnail (item.DefaultVersionUri, pixbuf);
		}

		static public bool ThumbnailIsValid (System.Uri uri, Gdk.Pixbuf thumbnail)
		{
			return FSpot.ThumbnailGenerator.ThumbnailIsValid (thumbnail, uri);
		}

		static public Gdk.Pixbuf ValidateThumbnail (string photo_path, Gdk.Pixbuf pixbuf)
		{			
			System.Uri uri = UriUtils.PathToFileUri (photo_path);
			return ValidateThumbnail (uri, pixbuf);
		}
		
		static public Gdk.Pixbuf ValidateThumbnail (System.Uri uri, Gdk.Pixbuf pixbuf)
		{			
			string thumbnail_path = Gnome.Thumbnail.PathForUri (uri.ToString (), 
									    Gnome.ThumbnailSize.Large);

			Gdk.Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

			if (pixbuf != null && thumbnail != null) {
				if (!ThumbnailIsValid (uri, thumbnail)) {
					Log.DebugFormat ("regenerating thumbnail for {0}", uri);
					FSpot.ThumbnailGenerator.Default.Request (uri, 0, 256, 256);
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
