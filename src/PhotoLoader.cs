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
			PixbufOrientation orientation = PixbufUtils.GetOrientation (photo.DefaultVersionPath);
			Gdk.Pixbuf orig = new Gdk.Pixbuf (photo.DefaultVersionPath);
			
			Gdk.Pixbuf rotated = PixbufUtils.TransformOrientation (orig, orientation, true);
			ValidateThumbnail (photo, rotated);
			if (rotated != orig)
				orig.Dispose ();

			return rotated;
		}

		static public Gdk.Pixbuf LoadAtMaxSize (Photo photo, int width, int height) 
		{
			Gdk.Pixbuf pixbuf = PixbufUtils.LoadAtMaxSize (photo.DefaultVersionPath, width, height);
			ValidateThumbnail (photo, pixbuf);
			return pixbuf;
		}

		static public Gdk.Pixbuf ValidateThumbnail (Photo photo, Gdk.Pixbuf pixbuf)
		{
			return ValidateThumbnail (photo.DefaultVersionPath, pixbuf);
		}

		static public Gdk.Pixbuf ValidateThumbnail (string photo_path, Gdk.Pixbuf pixbuf)
		{			
			string photo_uri = UriList.PathToFileUri (photo_path).ToString ();
			string thumbnail_path = Gnome.Thumbnail.PathForUri (photo_uri, 
									    Gnome.ThumbnailSize.Large);

			Gdk.Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

			if (pixbuf != null && thumbnail != null &&
			    pixbuf.Width >= THUMBNAIL_SIZE && pixbuf.Height >= THUMBNAIL_SIZE) {
				DateTime mtime = System.IO.File.GetLastWriteTime (photo_path);
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
					PhotoStore.ThumbnailFactory.SaveThumbnail (scaled, photo_uri, mtime);
					ThumbnailCache.Default.AddThumbnail (thumbnail_path, scaled);
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
