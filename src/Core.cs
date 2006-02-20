using System.IO;
using System;

namespace FSpot {
	[DBus.Interface ("org.gnome.FSpot.Core")]
	public abstract class CoreControl {
		[DBus.Method]
		public abstract void Import (string path);

		[DBus.Method]
		public abstract void Organize ();
		
		[DBus.Method]
		public abstract void View (string path);

		[DBus.Method]
		public abstract void Shutdown ();
	}

	public class Core : CoreControl
	{
		MainWindow organizer;
		private static Db db;
		static DBus.Connection connection;
		System.Collections.ArrayList toplevels;

		public Core ()
		{
			toplevels = new System.Collections.ArrayList ();
					
			// Load the database, upgrading/creating it as needed
			string base_directory = FSpot.Global.BaseDirectory;
			if (! File.Exists (base_directory))
				Directory.CreateDirectory (base_directory);
			
			db = new Db ();
			db.Init (Path.Combine (base_directory, "photos.db"), true);
		}

		public static DBus.Connection Connection {
			get {
				if (connection == null)
					connection = DBus.Bus.GetSessionBus ();

				return connection;
			}
		}

		public static Db Database {
			get { return db; }
		}

		public static Core FindInstance ()
		{
			DBus.Service service = DBus.Service.Get (Connection, "org.gnome.FSpot");
			return (Core)service.GetObject (typeof (Core), "/org/gnome/FSpot/Core");
		}

		public void RegisterServer ()
		{
			DBus.Service service = new DBus.Service (Connection, "org.gnome.FSpot");
			service.RegisterObject (this, "/org/gnome/FSpot/Core");
		}
		
		private class ImportCommand 
		{
			string path;
			MainWindow main;

			public ImportCommand (MainWindow main, string path) 
			{
				this.main = main;
				this.path = path;
			}

			public bool Execute ()
			{
				if (path != null && path.StartsWith ("gphoto2:"))
					main.ImportCamera (path);
				else
					main.ImportFile (path);
				
				return false;
			}
		}

		public override void Import (string path) 
		{
			ImportCommand cmd = new ImportCommand (MainWindow, path);
			//cmd.Execute ();
			GLib.Idle.Add (new GLib.IdleHandler (cmd.Execute));
		}

		public MainWindow MainWindow {
			get {
				if (organizer == null) {
					organizer = new MainWindow (db);
					Register (organizer.Window);
				}
				
				return organizer;
			}
		}
			
		public override void Organize ()
		{
			MainWindow.Window.Present ();
		}
		
		public override void View (string path)
		{
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path))
				Register (new FSpot.SingleView (path).Window);
			else {
				try {
					System.Uri uri = new System.Uri (path);
					path = uri.LocalPath;
					if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path))
						Register (new FSpot.SingleView (path).Window);
				} catch (System.Exception e) {
					System.Console.WriteLine (e.ToString ());
					System.Console.WriteLine ("no real valid path to view from {0}", path);
				}
			} 
		}
		
		private class SlideShow
		{
			SlideView slideview;

			public SlideShow (string name)
			{
				Tag tag;
				
				if (name != null)
					tag = db.Tags.GetTagByName (name);
				else {
					int id = (int) Preferences.Get (Preferences.SCREENSAVER_TAG);
					tag = db.Tags.GetTagById (id);
				}
				
				Photo [] photos = db.Photos.Query (new Tag [] { tag } );
				Array.Sort (photos, new Photo.RandomSort ());
				Gtk.Window window = new XScreenSaverSlide ();

				Gdk.Pixbuf black = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, 1, 1);
				black.Fill (0x00000000);
				slideview = new SlideView (black , photos);
				window.Add (slideview);
				window.ShowAll ();
			}

			public bool Execute ()
			{
				slideview.Play ();
				return false;
			}
		}

		public void ShowSlides (string name)
		{
			SlideShow show = new SlideShow (name);
			GLib.Idle.Add (new GLib.IdleHandler (show.Execute));
		}


		public override void Shutdown ()
		{
			System.Environment.Exit (0);
		}

		public void Register (Gtk.Window window)
		{
			toplevels.Add (window);
			window.Destroyed += HandleDestroyed;
		}

		public void HandleDestroyed (object sender, System.EventArgs args)
		{
			toplevels.Remove (sender);
			if (toplevels.Count == 0) {
				// FIXME
				// Should use Application.Quit(), but for that to work we need to terminate the threads
				// first too.
				System.Environment.Exit (0);
			}
			if (organizer.Window == sender)
				organizer = null;
		}
	}
}
