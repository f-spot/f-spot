using Gnome;
using System;
using System.IO;

public class Driver {

	public static void Main (string [] args)
	{
		Program program = new Program ("F-Spot", "0.0", Modules.UI, args);

		StockIcons.Initialize ();

		// FIXME: Error checking is non-existant here...

		string home_directory = Environment.GetEnvironmentVariable ("HOME");
		Directory.SetCurrentDirectory (home_directory);

		string base_directory = home_directory + "/.gnome2/f-spot";
		if (! File.Exists (base_directory))
			Directory.CreateDirectory (base_directory);

		Db db = new Db (base_directory + "/photos.db", true);

		MainWindow main_window = new MainWindow (db);
		main_window.Show ();

		program.Run ();
	}
}
