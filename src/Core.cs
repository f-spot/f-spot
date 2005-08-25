namespace FSpot {
	[DBus.Interface ("org.gnome.FSpot.Core")]
	public abstract class CoreControl {
		[DBus.Method]
		public abstract void Import (string path);

		[DBus.Method]
		public abstract void Organize ();
		
		[DBus.Method]
		public abstract void View (string path);
	}

	public class Core : CoreControl
	{
		MainWindow origanizer;
		Db db;
		System.Collections.ArrayList toplevels;
		static DBus.Connection connection;

		public Core (Db db)
		{
			this.db = db;
			toplevels = new System.Collections.ArrayList ();
		}

		public static DBus.Connection Connection {
			get {
				if (connection == null)
					connection = DBus.Bus.GetSessionBus ();

				return connection;
			}
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
				if (path.StartsWith ("gphoto2:"))
					main.ImportCamera (path);
				else
					main.ImportFile (path);
				
				return false;
			}
		}

		public override void Import (string path) 
		{
			ImportCommand cmd = new ImportCommand (MainWindow, path);
			GLib.Idle.Add (new GLib.IdleHandler (cmd.Execute));
		}

		public MainWindow MainWindow {
			get {
				if (origanizer == null) {
					origanizer = new MainWindow (db);
					Register (origanizer.Window);
				}
				
				return origanizer;
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
			else
				System.Console.WriteLine ("no valid path to import from");
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
		}
	}
}
