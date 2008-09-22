/*
 * FSpot.ThumbnailGenerator.cs
 *
 * Author(s)
 * 	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using FSpot.Utils;

namespace FSpot {
	public class ThumbnailGenerator : PixbufLoader {
		private static Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
		static public ThumbnailGenerator Default = new ThumbnailGenerator ();
		
		public const string ThumbMTime = "tEXt::Thumb::MTime";
		public const string ThumbUri = "tEXt::Thumb::URI";
		public const string ThumbImageWidth = "tEXt::Thumb::Image::Width";
		public const string ThumbImageHeight = "tEXt::Thumb::Image::Height"; 

		public static Gdk.Pixbuf Create (string path)
		{
			return Create (UriUtils.PathToFileUri (path));
		}
		
		public static Gdk.Pixbuf Create (Uri uri)
		{
			try {
				using (ImageFile img = ImageFile.Create (uri)) {
					Gdk.Pixbuf thumb = img.Load (256, 256);

					if (thumb != null)
						Save (thumb, uri);
					return thumb;
				}
			} catch {
				return null;
			}
		}
		
		public static bool ThumbnailIsValid (Gdk.Pixbuf thumbnail, System.Uri uri)
		{
			bool valid = false;

			try {	
				Gnome.Vfs.FileInfo vfs = new Gnome.Vfs.FileInfo (uri.ToString ());
				DateTime mtime = vfs.Mtime;
				valid  = Gnome.Thumbnail.IsValid (thumbnail, UriUtils.UriToStringEscaped (uri), mtime);
			} catch (System.IO.FileNotFoundException) {
				// If the original file is not on disk, the thumbnail is as valid as it's going to get
				valid = true;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				valid = false;
			}
			
			return valid;
		}

		public static string ThumbnailPath (System.Uri uri)
		{
			string large_path = Gnome.Thumbnail.PathForUri (UriUtils.UriToStringEscaped (uri), Gnome.ThumbnailSize.Large);
			return large_path;
		}

		[Obsolete ("Use ThumbnailPath (System.Uri) instead")]
		public static string ThumbnailPath (string path) 
		{
			return ThumbnailPath (UriUtils.PathToFileUri (path));
		}

		public static void Save (Gdk.Pixbuf image, Uri dest)
		{			
			string uri = UriUtils.UriToStringEscaped (dest);
			System.DateTime mtime = DateTime.Now;

			// Use Gnome.Vfs
			try {
				Gnome.Vfs.FileInfo vfs = new Gnome.Vfs.FileInfo (uri);
				mtime = vfs.Mtime;
	      
				PixbufUtils.SetOption (image, ThumbUri, uri);
				PixbufUtils.SetOption (image, ThumbMTime,
						       ((uint)GLib.Marshaller.DateTimeTotime_t (mtime)).ToString ());
			} catch (System.Exception e) {
				Console.WriteLine (e);
			}

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

		public override void Request (Uri uri, int order, int width, int height)
		{
			if (!uri.IsFile)
				Log.Debug ("FIXME: compute timestamp on non file uri too");
			if (uri.IsFile && System.IO.File.Exists (ThumbnailPath (uri)) 
				&& System.IO.File.GetLastWriteTime (ThumbnailPath (uri)) >= System.IO.File.GetLastWriteTime (uri.AbsolutePath))
				return;

			base.Request (uri, order, width, height);
		}

		protected override void ProcessRequest (RequestItem request)
		{
			try {
				base.ProcessRequest (request);

				Gdk.Pixbuf image = request.result;
				if (image != null)
					Save (image, request.uri);

				System.Threading.Thread.Sleep (75);
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}

	}
}
