using System;

using FSpot.Platform;
using FSpot.Utils;
using Hyena;

namespace FSpot {
	[Obsolete ("nuke or rename this")]
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
			using (ImageFile img = ImageFile.Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load ();
				ValidateThumbnail (item, pixbuf);
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf LoadAtMaxSize (IBrowsableItem item, int width, int height) 
		{
			using (ImageFile img = ImageFile.Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load (width, height);
				ValidateThumbnail (item.DefaultVersion.Uri, pixbuf);
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf ValidateThumbnail (IBrowsableItem item, Gdk.Pixbuf pixbuf)
		{
			return ValidateThumbnail (item.DefaultVersion.Uri, pixbuf);
		}

		static public Gdk.Pixbuf ValidateThumbnail (SafeUri uri, Gdk.Pixbuf pixbuf)
		{			
			using (Gdk.Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForUri (uri)) {
				if (pixbuf != null && thumbnail != null && !ThumbnailFactory.ThumbnailIsValid (thumbnail, uri)) {
					Log.Debug ("regenerating thumbnail for {0}", uri);
					FSpot.ThumbnailGenerator.Default.Request (uri, 0, 256, 256);
				}
			}
			return pixbuf;
		}

		public PhotoLoader (PhotoQuery query)
		{
			this.query = query;
		}
	}
}
