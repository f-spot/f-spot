//Author: Larry
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
			public Tests ()
			{
				Gnome.Vfs.Vfs.Initialize ();
				Gtk.Application.Init ();
			}
			
#if false
			[Test]
			public void TestLoad ()
			{
				JpegFile jimg = new JpegFile ("/home/lewing/start.swe.jpeg");
				Assert.AreEqual (PixbufOrientation.TopLeft, jimg.Orientation);
			}
#endif
			[Test]
			public void TestSave ()
			{
				string desc = "this is an example description";
				string desc2 = "\x00a9 Novell Inc.";
				PixbufOrientation orient = PixbufOrientation.TopRight;
				Gdk.Pixbuf test = new Gdk.Pixbuf (null, "f-spot-32.png");
				string path = ImageFile.TempPath ("joe.jpg");
				
				PixbufUtils.SaveJpeg (test, path, 75, new Exif.ExifData ());
				JpegFile jimg = new JpegFile (path);
				jimg.SetDescription (desc);
				jimg.SetOrientation (orient);
				jimg.SaveMetaData (path);
				JpegFile mod = new JpegFile (path);
				Assert.AreEqual (mod.Orientation, orient);
				Assert.AreEqual (mod.Description, desc);
				jimg.SetDescription (desc2);
				jimg.SaveMetaData (path);
				mod = new JpegFile (path);
				Assert.AreEqual (mod.Description, desc2);
				
				File.Delete (path);
			}
		}
#endif

