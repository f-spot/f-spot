//Author: Larry

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
				Gdk.Pixbuf test = new Gdk.Pixbuf (null, "f-spot-32.png");
				string path = ImageFile.TempPath ("joe.png");
				test.Save (path, "png");
				PngFile pimg = new PngFile (path);

				string desc = "this is a png test";
				string desc2 = "\000xa9 Novell Inc.";
				pimg.SetDescription (desc);
				using (Stream stream = File.OpenWrite (path)) {
					pimg.Save (stream);
				}
				PngFile mod = new PngFile (path);
				Assert.AreEqual (mod.Orientation, PixbufOrientation.TopLeft);
				Assert.AreEqual (mod.Description, desc);
				pimg.SetDescription (desc2);

				using (Stream stream = File.OpenWrite (path)) {
					pimg.Save (stream);
				}
				mod = new PngFile (path);
				Assert.AreEqual (mod.Description, desc2);
				
				File.Delete (path);
			}

			[Test]
			public void Load ()
			{
				string desc = "(c) 2004 Jakub Steiner\n\nCreated with The GIMP";
				Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly ();
				string path  = ImageFile.TempPath ("maddy.png");
				using (Stream output = File.OpenWrite (path)) {
					using (Stream source = assembly.GetManifestResourceStream ("f-spot-adjust-colors.png")) {
						byte [] buffer = new byte [256];
						while (source.Read (buffer, 0, buffer.Length) > 0) {
							output.Write (buffer, 0, buffer.Length);
						}
					}
				}
				PngFile pimg = new PngFile (path);
				Assert.AreEqual (pimg.Description, desc);

				File.Delete (path);
			}
		}
#endif

#if false
		public class ImageFile {
			string Path;
			public ImageFile (string path)
			{
				this.Path = path;
			}
		}

		public static void Main (string [] args) 
		{
			System.Collections.ArrayList failed = new System.Collections.ArrayList ();
			Gtk.Application.Init ();
			foreach (string path in args) {
				Gtk.Window win = new Gtk.Window (path);
				Gtk.HBox box = new Gtk.HBox ();
				box.Spacing = 12;
				win.Add (box);
				Gtk.Image image;
				image = new Gtk.Image ();

				System.DateTime start = System.DateTime.Now;
				System.TimeSpan one = start - start;
				System.TimeSpan two = start - start;
				try {
					start = System.DateTime.Now;
					image.Pixbuf = new Gdk.Pixbuf (path);
					one = System.DateTime.Now - start;
				}  catch (System.Exception e) {
				}
				box.PackStart (image);

				image = new Gtk.Image ();
				try {
					start = System.DateTime.Now;
					PngFile png = new PngFile (path);
					image.Pixbuf = png.GetPixbuf ();
					two = System.DateTime.Now - start;
				} catch (System.Exception e) {
					failed.Add (path);
					//System.Console.WriteLine ("Error loading {0}", path);
					System.Console.WriteLine (e.ToString ());
				}

				System.Console.WriteLine ("{2} Load Time {0} vs {1}", one.TotalMilliseconds, two.TotalMilliseconds, path); 
				box.PackStart (image);
				win.ShowAll ();
			}
			
			System.Console.WriteLine ("{0} Failed to Load", failed.Count);
			foreach (string fail_path in failed) {
				System.Console.WriteLine (fail_path);
			}

			Gtk.Application.Run ();
		}
#endif

