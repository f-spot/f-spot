#if ENABLE_NUNIT
using NUnit.Framework;
using System;
using System.IO;

using FSpot.Utils;

namespace FSpot.Tests
{
	[TestFixture]
	public class ThumbnailGeneratorTests {
		string [] Names = new string [] { 
			"\x00a9F-SpotUnit\x00b5Test.png",
			"img(\x00a9F-SpotUnit\x00b5Test).png",
			"img\u00ff.png",
			"img\u0100.png",
			"imgο.png",
			"img(ο).png",
			"img(τροποποιημένο).png",
		};

		public ThumbnailGeneratorTests ()
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
			System.Uri uri = UriUtils.PathToFileUri (path);

			Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);
			string escaped = UriUtils.PathToFileUriEscaped (path);
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
				Assert.AreEqual (thumb.GetOption (ThumbnailGenerator.ThumbUri), UriUtils.PathToFileUriEscaped (path));
				Assert.AreEqual (new Uri (thumb.GetOption (ThumbnailGenerator.ThumbUri)), UriUtils.PathToFileUri (path));
				Assert.IsTrue (ThumbnailGenerator.ThumbnailIsValid (thumb, UriUtils.PathToFileUri (path)));
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
				Assert.AreEqual (thumb.GetOption (ThumbnailGenerator.ThumbUri), UriUtils.UriToStringEscaped (uri));
				Assert.AreEqual (new Uri (thumb.GetOption (ThumbnailGenerator.ThumbUri)), uri);
				Assert.IsTrue (ThumbnailGenerator.ThumbnailIsValid (thumb, uri));
			}

			File.Delete (thumb_path);
			File.Delete (path);
		}
	}
}
#endif 

