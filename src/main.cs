using Gtk;
using Gnome;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using Mono.Unix;

namespace FSpot 
{

public class Driver {
	
	private static void HelpMsg(string [] args)
	{
		Program program = null;
		Catalog.Init ("f-spot", Defines.LOCALE_DIR);

		System.Console.WriteLine ("Usage: f-spot [--basedir <directory>] | [--photodir <directory>] | [--debug]");
		System.Console.WriteLine ("\t\t [ --import <uri> | --view <file[ file]|directory[ directory]> |");
		System.Console.WriteLine ("\t\t\t--shutdown | --slideshow | --help ]");
		System.Console.WriteLine ("");

		System.Console.WriteLine ("  --import [uri]\t\t\timport from the given uri");
		System.Console.WriteLine ("  --view <file|directory>\t\tview some files or directories (one or more)");
		System.Console.WriteLine ("  --basedir <directory>\t\t\t<dir> where the photo database is located ");
		System.Console.WriteLine ("  --photodir <directory>\t\timport (copy) photos to <dir> ");
		System.Console.WriteLine ("  --shutdown\t\t\t\tshutdown a running f-spot server");
		System.Console.WriteLine ("  --slideshow\t\t\t\tdisplay a slideshow");
		System.Console.WriteLine ("  --debug\t\t\t\trun f-spot with mono in debug mode");
		System.Console.WriteLine ("  --help\t\t\t\tview this message");

	}
	
	private static bool ValidateCmds(string [] args)
	{
		string [] restricted_cmds = new string [] {"--slideshow", "--import", "--shutdown", "--view"};
		string [] valid_cmds = new string [] {"--import", "--view", "--basedir", "--photodir", 
			"--shutdown", "--slideshow", "--debug", "--help", "--uninstalled"};
		string [] single_cmds = new string [] {"--shutdown", "--help"};
		string [] parameter_cmds = new string [] {"--import", "--view", "--basedir", "--photodir"};


		bool valid_cmd = true;
		int number_of_restricted_cmds = 0;
		for (int k=0; k< args.Length; k++) {
			string arg = args[k];
			// Only check commands
			if (!arg.StartsWith("--"))
				continue;
			// Only one restricted cmd is allowed
			if (System.Array.IndexOf (restricted_cmds, arg) > 0)
				number_of_restricted_cmds++;  
			// These commands has to be by itself
			if ( (System.Array.IndexOf (single_cmds, arg) > 0) && (args.Length != 1) )
				valid_cmd = false; 
			// Unknow command is not allowed
			if (System.Array.IndexOf (valid_cmds, arg) < 0)			
				valid_cmd = false; 
			// Command with a parameter has to have a parameter
			if (System.Array.IndexOf (parameter_cmds, arg) > 0) {
				if ( (k+1 == args.Length) || (args[k+1].StartsWith ("--"))  )
					valid_cmd = false;
			}

		}
		valid_cmd = valid_cmd && (number_of_restricted_cmds < 2);
		
		return valid_cmd;		
	}
	
	public static void Main (string [] args)
	{
		bool view_only = false;
		bool import = false;
		bool empty = false;
		Program program = null;
		ICore control = null;

		SetProcessName (Defines.PACKAGE);
		
		NDesk.DBus.BusG.Init();
		try {
			if (!ValidateCmds(args)) {
				System.Console.WriteLine ("Invalid argument list. Please review the arguments and try again.");
				System.Console.WriteLine ("");
				HelpMsg(args);
				return;
			}
			
			foreach (string arg in args) {
				if (arg == "--help") {
					HelpMsg(args);
					return;
				} else if (arg == "--slideshow") {
					Catalog.Init ("f-spot", Defines.LOCALE_DIR);
						
					program = new Program (Defines.PACKAGE, 
							       Defines.VERSION, 
						       Modules.UI, args);
					Core core = new Core ();
					core.ShowSlides (null);
					program.Run ();
					System.Console.WriteLine ("done");
					return;
				}
			}
			
			/* 
			 * FIXME we need to inialize gobject before making the dbus calls, we'll go 
			 * ahead and do it like this for now.
			 */ 
			program = new Program (Defines.PACKAGE, 
					       Defines.VERSION, 
					       Modules.UI, args);		
			
			bool create = true;
			while (control == null) {
				try {
					control = Core.FindInstance ();
					System.Console.WriteLine ("Found active FSpot server: {0}", control);
					program = null;
				} catch (System.Exception e) { 
					if (System.Array.IndexOf (args, "--shutdown") == 0)
						System.Console.WriteLine ("Starting new FSpot server");
				}
				
				// Process this before creating the Core
				for (int i = 0; i < args.Length; i++) {
					switch (args[i]) {
					case "--basedir":
							if (++i < args.Length) {
								FSpot.Global.BaseDirectory = args [i];
								System.Console.WriteLine("BaseDirectory is now {0}", args[i]);
							}
						break;
					case "--photodir":
							if (++i < args.Length) {
								FSpot.Global.PhotoDirectory = System.IO.Path.GetFullPath(args [i]);
								System.Console.WriteLine("PhotoDirectory is now {0}", System.IO.Path.GetFullPath(args[i]));
							}
						break;
					}
				}

				Core core = null;
				try {
					if (control == null && create) {
						create = false;
						Gnome.Vfs.Vfs.Initialize ();
						StockIcons.Initialize ();
						
						Catalog.Init ("f-spot", Defines.LOCALE_DIR);
						Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {PixbufUtils.LoadFromAssembly ("f-spot-logo.png")};
						
						core = new Core ();
						core.RegisterServer ();
						
						// FIXME: Error checking is non-existant here...
						
						empty = Core.Database.Empty;
						control = core;
					}
				} catch (System.Exception e) {
					System.Console.WriteLine ("XXXXX\n{0}\nXXXXX", e);
					control = null;

					if (core != null)
						core.UnregisterServer ();
				}
			}
			
			
			UriList list = new UriList ();
			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
				case "--shutdown":
					try {
						control.Shutdown ();
					} catch (System.Exception) {
						// trap errors
					}
					System.Environment.Exit (0);
					break;
				case "--import":
					if (++i < args.Length)
						control.Import (args [i]);
					
					import = true;
					break;
				case "--view":
					while (++i < args.Length) {
						if (args[i].StartsWith ("--"))
							break;
						list.AddUnknown (args [i]);
					}
					
					if (list.Count > 0)
						control.View (list.ToString ());

					view_only = true;
					break;
				}
			}
			
			if (empty && !import)
				control.Import (null);
			
			if (import || !view_only) {
				control.Organize ();
				Gdk.Global.NotifyStartupComplete ();
			}			

			if (program != null)
				program.Run ();
			
			System.Console.WriteLine ("exiting");
		} catch (System.Exception e) {
			Console.Error.WriteLine(e);
			Gtk.Application.Init();
			ExceptionDialog dlg = new ExceptionDialog(e);
			dlg.Run();
			dlg.Destroy();
			System.Environment.Exit(1);
		}
	}

	[DllImport("libc")]
	private static extern int prctl(int option, string name, ulong arg3,
					ulong arg4, ulong arg5);
	
	public static void SetProcessName(string name)
	{
		try {
			if(prctl(15 /* PR_SET_NAME */, name,
				 0, 0, 0) != 0) {
				Console.WriteLine ("Error setting process name: " +
						   Mono.Unix.Native.Stdlib.GetLastError());
			}
		} catch (DllNotFoundException de) {
			/* noop */
		} catch (EntryPointNotFoundException de) {
		    	/* noop */
		}
	}
}

}
