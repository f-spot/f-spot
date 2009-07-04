#if ENABLE_NUNIT
using NUnit.Framework;
using System;
using System.IO;

using FSpot.Utils;
using FSpot.Platform;

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
			Uri fileuri = UriUtils.PathToFileUri (path);
			//string thumb_path = ThumbnailFactory.PathForUri.ThumbnailPath (UriUtils.PathToFileUri(path));
			

			using (Gdk.Pixbuf thumb = ThumbnailGenerator.Create (fileuri)) {
				Assert.IsTrue (ThumbnailFactory.ThumbnailExists (fileuri), String.Format ("Missing: thumbnail created from {0}", fileuri));
				Assert.IsNotNull (thumb);
				Assert.AreEqual (thumb.GetOption (ThumbnailGenerator.ThumbUri), UriUtils.PathToFileUriEscaped (path));
				Assert.AreEqual (new Uri (thumb.GetOption (ThumbnailGenerator.ThumbUri)), fileuri);
				Assert.IsTrue (ThumbnailFactory.ThumbnailIsValid (thumb, fileuri));
			}
			
			ThumbnailFactory.DeleteThumbnail (fileuri);
			File.Delete (path);
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

//			string string_path = ThumbnailGenerator.ThumbnailPath (path);
//			string thumb_path = ThumbnailGenerator.ThumbnailPath (uri);
//			Assert.AreEqual (thumb_path, string_path);

			ThumbnailGenerator.Create (uri);

			using (Gdk.Pixbuf thumb = ThumbnailGenerator.Create (uri)) {
				Assert.IsTrue (ThumbnailFactory.ThumbnailExists (uri));
				Assert.IsNotNull (thumb);
				Assert.AreEqual (thumb.GetOption (ThumbnailGenerator.ThumbUri), UriUtils.UriToStringEscaped (uri));
				Assert.AreEqual (new Uri (thumb.GetOption (ThumbnailGenerator.ThumbUri)), uri);
				Assert.IsTrue (ThumbnailFactory.ThumbnailIsValid (thumb, uri));
			}

			ThumbnailFactory.DeleteThumbnail (uri);
			File.Delete (path);
		}
	}
}
#endif 

