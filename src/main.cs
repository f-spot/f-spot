using Gtk;
using Gnome;
using System;
using System.IO;

public class Driver {

	public static void Main (string [] args)
	{
		Application.Init ();
		Program program = new Program (FSpot.Defines.PACKAGE, FSpot.Defines.VERSION, Modules.UI, args);

		Gnome.Vfs.Vfs.Initialize ();
		StockIcons.Initialize ();

		Mono.Posix.Catalog.Init ("f-spot", FSpot.Defines.LOCALE_DIR);

		// FIXME: Error checking is non-existant here...

		string base_directory = FSpot.Global.BaseDirectory;
		if (! File.Exists (base_directory))
			Directory.CreateDirectory (base_directory);

		Db db = new Db (Path.Combine (base_directory, "photos.db"), true);

		Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {PixbufUtils.LoadFromAssembly ("f-spot-logo.png")};

		bool view_only = false;
		bool import = db.Empty;
		string import_path = null;

		for (int i = 0; i < args.Length; i++) {
			switch (args [i]) {
			case "--import":
				if (++i < args.Length && (args [i].StartsWith ("gphoto2:") || File.Exists (args [i]) || Directory.Exists (args[i]))) {
					import_path = args [i];
				} else {
					System.Console.WriteLine ("no valid path to import from");
				}
				import = true;
				break;
			case "--view":
				if (++i < args.Length && (File.Exists (args [i]) || Directory.Exists (args[i]))) {
					new FSpot.SingleView (args [i]);
					view_only = true;
				} else {
					System.Console.WriteLine ("no valid path to import from");
				}
				break;
			}

		}

		if (!view_only) {
			new MainWindow (db);
			if (import) {
				if (import_path != null && import_path.StartsWith ("gphoto2:"))
					MainWindow.Toplevel.ImportCamera (import_path);
				else
					MainWindow.Toplevel.ImportFile (import_path);
			}
		}

		program.Run ();
	}
}
