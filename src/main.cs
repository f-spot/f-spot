using Gtk;
using Gnome;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class Driver {
	public static void Main (string [] args)
	{
		bool view_only = false;
		bool import = false;
		bool empty = false;
		Program program = null;
		FSpot.CoreControl control = null;

		SetProcessName (FSpot.Defines.PACKAGE);
		
		foreach (string arg in args) {
			if (arg == "--help") {
				System.Console.WriteLine ("Usage f-spot [OPTION. ..]\n");
				System.Console.WriteLine ("  --import [uri]\t\t\timport from the given uri");
				System.Console.WriteLine ("  --view <file>\t\t\t\tview a file or directory ");
				System.Console.WriteLine ("  --shutdown\t\t\t\tshutdown a running f-spot server");
				System.Console.WriteLine ("  --help\t\t\t\tview this message");
				System.Console.WriteLine ("");

				program = new Program (FSpot.Defines.PACKAGE, 
						       FSpot.Defines.VERSION, 
						       Modules.UI, args);
				return;
			}
		}


		try {
			control = FSpot.Core.FindInstance ();
			System.Console.WriteLine ("Found active FSpot server: {0}", control);
		} catch (System.Exception e) { 
			System.Console.WriteLine ("Starting new FSpot server");
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
			
			core = new FSpot.Core ();

			try {
				core.RegisterServer ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}

			empty = FSpot.Core.Database.Empty;
			control = core;
		}
			
		for (int i = 0; i < args.Length; i++) {
			switch (args [i]) {
			case "--help":
				System.Console.WriteLine ("Usage f-spot [command [options]]\n");
				System.Console.WriteLine ("--import [uri]\timport from the given uri");
				System.Console.WriteLine ("--view <file>\tview a file or directory ");
				System.Console.WriteLine ("--shutdown\tshutdown a running f-spot server");
				System.Console.WriteLine ("--help\tview this message");
				break;
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

	[DllImport("libc")]
	private static extern int prctl(int option, string name, ulong arg3,
					ulong arg4, ulong arg5);
	
	public static void SetProcessName(string name)
	{
		try {
			if(prctl(15 /* PR_SET_NAME */, name,
				 0, 0, 0) != 0) {
				throw new ApplicationException("Error setting process name: " +
							       Mono.Unix.Native.Stdlib.GetLastError());
			}
		} catch (DllNotFoundException de) {
			/* noop */
		}
	}
}
