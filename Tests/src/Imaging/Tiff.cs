// Author: Larry

#if ENABLE_NUNIT
	[TestFixture]
	public class Tests {
		public Tests ()
		{
			Gnome.Vfs.Vfs.Initialize ();
			Gtk.Application.Init ();
		}
		
		[Test]
		public void Save ()
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
			
			Header header = mod.ExifHeader;
#if USE_TEST_FILE
			string tmp = "/home/lewing/test.tiff";
			if (File.Exists (tmp))
				File.Delete (tmp);
			Stream stream = File.Open (tmp, FileMode.Create, FileAccess.ReadWrite);
			Console.WriteLine ("XXXX saving tiff {0}", tmp);
#else
			System.IO.MemoryStream stream = new System.IO.MemoryStream ();
#endif

			header.Dump ("source");
			header.Save (stream);
			stream.Position = 0;
			System.Console.WriteLine ("----------------------------------------------LOADING TIFF");
			Header loader = new Header (stream);
			loader.Dump ("loader");
			
			CompareDirectories (header.Directory, loader.Directory);

			System.IO.File.Delete (path);	
		}

		private void CompareDirectories (ImageDirectory olddir, ImageDirectory newdir)
		{
			Assert.AreEqual (olddir.Entries.Count, newdir.Entries.Count);
			for (int i = 0; i < olddir.Entries.Count; i++) {
				Assert.AreEqual (olddir.Entries [i].Id, newdir.Entries [i].Id);
				Assert.AreEqual (olddir.Entries [i].Type, newdir.Entries [i].Type);
				Assert.AreEqual (olddir.Entries [i].Count, newdir.Entries [i].Count);
				Assert.AreEqual (olddir.Entries [i].Length, newdir.Entries [i].Length);
				if (olddir.Entries [i] is SubdirectoryEntry) {
					SubdirectoryEntry oldsub = olddir.Entries [i] as SubdirectoryEntry;
					SubdirectoryEntry newsub = newdir.Entries [i] as SubdirectoryEntry;
					
					for (int j = 0; j < oldsub.Directory.Length; j++)
						CompareDirectories (oldsub.Directory [j], newsub.Directory [j]);
				}
			}
		}
	}
#endif

