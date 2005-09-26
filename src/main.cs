using Gtk;
using Gnome;
using System;
using System.IO;

public class Driver {

	public static void Main (string [] args)
	{
		bool view_only = false;
		bool import = false;
		bool empty = false;
		Program program = null;
		FSpot.CoreControl control = null;

		try {
			control = FSpot.Core.FindInstance ();
			System.Console.WriteLine ("Found active FSpot server: {0}", control);
		} catch (System.Exception e) { 
			System.Console.WriteLine ("Unable to find active server: {0}", e.Message);
		}

		if (control == null) {
			FSpot.Core core = null;
			program = new Program (FSpot.Defines.PACKAGE, 
					       FSpot.Defines.VERSION, 
					       Modules.UI, args);
			
			Gnome.Vfs.Vfs.Initialize ();
			StockIcons.Initialize ();
			
			Mono.Posix.Catalog.Init ("f-spot", FSpot.Defines.LOCALE_DIR);
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {PixbufUtils.LoadFromAssembly ("f-spot-logo.png")};
			
			// FIXME: Error checking is non-existant here...
			
			string base_directory = FSpot.Global.BaseDirectory;
			if (! File.Exists (base_directory))
				Directory.CreateDirectory (base_directory);
			
			Db db = new Db (Path.Combine (base_directory, "photos.db"), true);
			core = new FSpot.Core (db);

			try {
				core.RegisterServer ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}

			empty = db.Empty;
			control = core;
		}			
			
		for (int i = 0; i < args.Length; i++) {
			switch (args [i]) {
			case "--shutdown":
				control.Shutdown ();
				break;
			case "--import":
				if (++i < args.Length)
					control.Import (args [i]);

				import = true;
				break;
			case "--view":
				if (++i < args.Length)
					control.View (args [i]);

				view_only = true;
				break;
			}
		}

		if (empty && !import)
			control.Import (null);

		if (import || !view_only)
			control.Organize ();
		
		if (program != null)
			program.Run ();
	}
}
