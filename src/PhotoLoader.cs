using System;

namespace FSpot 
{
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
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (photo.DefaultVersionPath);
			ValidateThumbnail (photo, pixbuf);
			return pixbuf;
		}

		static public Gdk.Pixbuf LoadAtMaxSize (Photo photo, int width, int height) 
		{
			Gdk.Pixbuf pixbuf = PixbufUtils.LoadAtMaxSize (photo.DefaultVersionPath, width, height);
			ValidateThumbnail (photo, pixbuf);
			return pixbuf;
		}


		static private Gdk.Pixbuf ValidateThumbnail (Photo photo, Gdk.Pixbuf pixbuf)
		{
			string photo_path = photo.DefaultVersionPath;
			string photo_uri = "file://" + photo_path;
			string thumbnail_path = Gnome.Thumbnail.PathForUri (photo_uri, 
									    Gnome.ThumbnailSize.Large);

			Gdk.Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

			if (pixbuf != null && thumbnail != null &&
			    pixbuf.Width >= THUMBNAIL_SIZE && pixbuf.Height >= THUMBNAIL_SIZE) {
				DateTime mtime = System.IO.File.GetLastWriteTime (photo.DefaultVersionPath);
				bool valid = false;

				try {
					Console.WriteLine ("uri \"{0}\" mtime \"{1}\"", 
							   thumbnail.GetOption ("tEXt::Thumb::URI"), 
							   thumbnail.GetOption ("tEXt::Thumb::MTime"));
					// Fixme This shouldn't be throwing an exception but it is
					valid  = Gnome.Thumbnail.IsValid (thumbnail, photo_uri, mtime);

					
				} catch (Exception e) {
					Console.WriteLine (e);
					valid = false;
				}
					
				if (!valid) {
					Console.WriteLine ("regenerating thumbnail");
			
					Gdk.Pixbuf scaled = PixbufUtils.ScaleToMaxSize (pixbuf, THUMBNAIL_SIZE, THUMBNAIL_SIZE);
					PixbufUtils.SetOption (scaled, "tEXt::Thumb::URI", photo_uri);
					PixbufUtils.SetOption (scaled, "tEXt::Thumb::MTime", 
							       ((uint)GLib.Marshaller.DateTimeTotime_t (mtime)).ToString ());
					PhotoStore.thumbnail_factory.SaveThumbnail (scaled, photo_uri, mtime);
					ThumbnailCache.Default.AddThumbnail (thumbnail_path, scaled);
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
