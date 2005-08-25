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
		static DBus.Connection connection;

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

		public Core (Db db)
		{
			this.db = db;
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
				if (origanizer == null)
					origanizer = new MainWindow (db);
				
				return origanizer;
			}
		}
			
		public override void Organize () 
		{
			MainWindow.Present ();
		}
		
		public override void View (string path)
		{
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path))
				new FSpot.SingleView (path);
			else
				System.Console.WriteLine ("no valid path to import from");
		}
	}
}
