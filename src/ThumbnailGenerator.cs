using System;
using System.IO;

namespace FSpot {
	public class ThumbnailGenerator : PixbufLoader {
		private static Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
		static public ThumbnailGenerator Default = new ThumbnailGenerator ();
		
		public static Gdk.Pixbuf Create (string path)
		{
			if (File.Exists (path))
				return Create (UriList.PathToFileUri (path));
			
			return Create (new Uri (path));
		}
		
		public static Gdk.Pixbuf Create (Uri uri)
		{
			try {
				ImageFile img = ImageFile.Create (uri);
				Gdk.Pixbuf thumb = img.Load (256, 256);

				if (thumb != null)
					Save (thumb, uri);
				return thumb;
			} catch {
				return null;
			}
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

		public static void Save (Gdk.Pixbuf image, Uri dest)
		{			
			string uri = dest.ToString ();
			// Use Gnome.Vfs
			System.DateTime mtime = System.IO.File.GetLastWriteTime (dest.LocalPath);
			
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
			try {
				base.ProcessRequest (request);

				Gdk.Pixbuf image = request.result;
				if (image != null) {
					Uri uri;
					if (File.Exists (request.path))
						uri = UriList.PathToFileUri (request.path);
					else
						uri = new Uri (request.path);

					Save (image, uri);
				}

				System.Threading.Thread.Sleep (75);
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}
	}
}
