using System;
using System.IO;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

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
			return Create (UriList.PathToFileUri (path));
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
				valid  = Gnome.Thumbnail.IsValid (thumbnail, UriList.UriToStringEscaped (uri), mtime);
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				valid = false;
			}
			
			return valid;
		}

		public static string ThumbnailPath (System.Uri uri)
		{
			string large_path = Gnome.Thumbnail.PathForUri (UriList.UriToStringEscaped (uri), Gnome.ThumbnailSize.Large);
			return large_path;
		}

		public static string ThumbnailPath (string path) 
		{
			return ThumbnailPath (UriList.PathToFileUri (path));
		}

		public static void Save (Gdk.Pixbuf image, Uri dest)
		{			
			string uri = UriList.UriToStringEscaped (dest);
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

#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
			string [] Names = new string [] { 
				"\x00a9F-SpotUnit\x00b5Test.png",
				"img(\x00a9F-SpotUnit\x00b5Test).png",
				"img\u00ff.png",
				"img\u0100.png",
				"imgο.png",
				"img(ο).png",
				"img(τροποποιημένο).png",
			};

			public Tests ()
			{
				Gnome.Vfs.Vfs.Initialize ();
			}

			public string CreateFile (string name, int size)
			{
				using (Gdk.Pixbuf test = new Gdk.Pixbuf (null, "f-spot-32.png")) {
					using (Gdk.Pixbuf tmp = test.ScaleSimple (size, size, Gdk.InterpType.Nearest)) {
						string path = System.IO.Path.GetTempPath ();
						path = System.IO.Path.Combine (path, name);
						Console.WriteLine (path);
						tmp.Save (path, Path.GetExtension (path).TrimStart (new char [] { '.' }));
						return path;
					}
				}
			}
			
			[Test]
			public void BadNames ()
			{
				foreach (string name in Names) {
					BadNames (name);
				}
			}

			public void BadNames (string name)
			{
				string path = CreateFile (name, 512);
				System.Uri uri = UriList.PathToFileUri (path);
	
				Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
				string escaped = UriList.PathToFileUriEscaped (path);
				string large_path = Gnome.Thumbnail.PathForUri (escaped,
										Gnome.ThumbnailSize.Large);

				using (Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (uri.LocalPath)) {
					factory.SaveThumbnail (pixbuf, escaped, System.DateTime.Now);
					
					Assert.IsTrue (File.Exists (large_path), String.Format ("Missing: {0} created from {1} as {2}", large_path, path, escaped));
					Gdk.Pixbuf thumb = new Gdk.Pixbuf (large_path);
					Assert.IsNotNull (thumb);
				}

				File.Delete (path);
				File.Delete (large_path);
			}

			[Test]
			public void StringNames ()
			{
				foreach (string name in Names) {
					StringNames (name);
				}
			}
			
			public void StringNames (string name)
			{
				string path = CreateFile (name, 1024);
				string thumb_path = ThumbnailGenerator.ThumbnailPath (path);
				
				ThumbnailGenerator.Create (path);

				Assert.IsTrue (File.Exists (thumb_path), String.Format ("Missing: {0} created from {1}", thumb_path, path));
				using (Gdk.Pixbuf thumb = new Gdk.Pixbuf (thumb_path)) {
					Assert.IsNotNull (thumb);
					Assert.AreEqual (thumb.GetOption (ThumbUri), UriList.PathToFileUriEscaped (path));
					Assert.AreEqual (new Uri (thumb.GetOption (ThumbUri)), UriList.PathToFileUri (path));
					Assert.IsTrue (ThumbnailGenerator.ThumbnailIsValid (thumb, UriList.PathToFileUri (path)));
				}
				
				File.Delete (path);
				File.Delete (thumb_path);
			}

			[Test]
			public void UriNames ()
			{
				foreach (string name in Names) {
					UriNames (name);
				}
			}

			public void UriNames (string name)
			{
				string path = CreateFile (name, 768);
				Uri uri = new Uri (Gnome.Vfs.Uri.GetUriFromLocalPath (path));

				string string_path = ThumbnailGenerator.ThumbnailPath (path);
				string thumb_path = ThumbnailGenerator.ThumbnailPath (uri);
				Assert.AreEqual (thumb_path, string_path);

				ThumbnailGenerator.Create (uri);

				Assert.IsTrue (File.Exists (thumb_path), String.Format ("Missing: {0} created from {1}", thumb_path, uri));
				using (Gdk.Pixbuf thumb = new Gdk.Pixbuf (thumb_path)) {
					Assert.IsNotNull (thumb);
					Assert.AreEqual (thumb.GetOption (ThumbUri), UriList.UriToStringEscaped (uri));
					Assert.AreEqual (new Uri (thumb.GetOption (ThumbUri)), uri);
					Assert.IsTrue (ThumbnailGenerator.ThumbnailIsValid (thumb, uri));
				}

				File.Delete (thumb_path);
				File.Delete (path);
			}
		}
#endif 
	}
}
