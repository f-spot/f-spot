using Gtk;
using Gnome;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Mono.Addins;
using Mono.Addins.Setup;

namespace FSpot 
{
public class Driver {
	static void Help ()
	{
		Console.WriteLine ("F-Spot  {0} - (c)2003-2007, Novell Inc\n" +
			"Personal photo management for the GNOME Desktop\n\n" +
			"Usage: f-spot [options] \n" +
			"Options:\n" +
		  	"-b -basedir PARAM   path to the photo database folder\n" +
		    	"-? -help -usage     Show this help list\n" +
			"-i -import PARAM    import from the given uri\n" +
			"-p -photodir PARAM  default import folder\n" +
			"-shutdown           shutdown a running f-spot instance\n" +
			"-slideshow          display a slideshow\n" +
			"-V -version         Display version and licensing information\n" +
			"-v -view            view file(s) or directory(ies)\n", FSpot.Defines.VERSION);
	}

	static int Main (string [] args)
	{
		bool empty = false;
		Program program = null;
		ICore control = null;
		List<string> uris = new List<string> ();
		SetProcessName (Defines.PACKAGE);

		// Options and Option parsing
		bool shutdown = false;
		bool view = false;
		bool slideshow = false;
		string import_uri = null;
		string view_uri = null;

		for (int i = 0; i < args.Length && !shutdown; i++) {
			switch (args [i]) {
			case "-h": case "-?": case "-help": case "--help": case "-usage":
				Help ();
				return 0;
			
			case "-shutdown":
				System.Console.WriteLine ("Shutting down existing F-Spot server...");
				shutdown = true;
				break;

			case "-b": case "-basedir": case "--basedir":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Console.WriteLine ("f-spot: -basedir option takes one argument");
					return 1;
				}
				FSpot.Global.BaseDirectory = args [++i];
				System.Console.WriteLine ("BaseDirectory is now {0}", FSpot.Global.BaseDirectory);
				break;

			case "-p": case "-photodir": case "--photodir":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Console.WriteLine ("f-spot: -photodir option takes one argument");
					return 1;
				}
				FSpot.Global.PhotoDirectory = System.IO.Path.GetFullPath (args [++i]);
				System.Console.WriteLine ("PhotoDirectory is now {0}", FSpot.Global.PhotoDirectory);
				break;

			case "-i": case "-import": case "--import":
				if (i+1 == args.Length) {
					Console.WriteLine ("f-spot: -import option takes one argument");
					return 1;
				}
				import_uri = args [++i];
				break;
			
			case "-slideshow":
				slideshow = true;
				break;

			case "-v": case "-view": case "--view":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Console.WriteLine ("f-spot: -view option takes one argument");
					return 1;
				}
				if (!System.IO.Directory.Exists (args[i+1]) && !System.IO.File.Exists (args[i+1])) {
					Console.WriteLine ("f-spot: -view argument must be an existing file or directory");
					return 1;
				}
				view = true;
				uris.Add (args [++i]);
				break;
			
			case "--debug": case "--trace": case "--profile": case "--uninstalled":
				break;

			default:
				Help ();
				return 1;
				break;
			}
		}

		// Validate command line options
		if ( (import_uri != null && (view || shutdown || slideshow)) || 
		     (view && (shutdown || slideshow)) ||
		     (shutdown && slideshow) ) {
			Console.WriteLine ("Can't mix -import, -view, -shutdown or -slideshow");
			return 1;
		}

		if (slideshow == true) {
			Catalog.Init ("f-spot", Defines.LOCALE_DIR);
				
			program = new Program (Defines.PACKAGE, 
					       Defines.VERSION, 
					       Modules.UI, args);
			Core core = new Core ();
			core.ShowSlides (null);
			program.Run ();
			System.Console.WriteLine ("done");
			return 0;
		}
		
		try {

			try {
				NDesk.DBus.BusG.Init();
			} catch (Exception e) {
				throw new ApplicationException ("F-Spot cannot find the Dbus session bus.  Make sure dbus is configured properly or start a new session for f-spot using \"dbus-launch f-spot\"", e);
			}
			/* 
			 * FIXME we need to inialize gobject before making the dbus calls, we'll go 
			 * ahead and do it like this for now.
			 */ 
			program = new Program (Defines.PACKAGE, 
					       Defines.VERSION, 
					       Modules.UI, args);		
			
			Console.WriteLine ("Initializing Mono.Addins");
			AddinManager.Initialize (FSpot.Global.BaseDirectory);
			AddinManager.Registry.Update (null);
			SetupService setupService = new SetupService (AddinManager.Registry);
			setupService.Repositories.RegisterRepository (null, "http://addins.f-spot.org", false);

			bool create = true;
			int retry_count = 0;
			while (control == null) {
				try {
					control = Core.FindInstance ();
					System.Console.WriteLine ("Found active FSpot server: {0}", control);
					program = null;
				} catch (System.Exception) { 
					if (!shutdown)
						System.Console.WriteLine ("Starting new FSpot server");
				}
				
				Core core = null;
				try {
					if (control == null && create) {
						create = false;
						Gnome.Vfs.Vfs.Initialize ();
						
						Catalog.Init ("f-spot", Defines.LOCALE_DIR);
						try {
							Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {
								GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 16, (Gtk.IconLookupFlags)0),
								GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 22, (Gtk.IconLookupFlags)0),
								GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 32, (Gtk.IconLookupFlags)0),
								GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 48, (Gtk.IconLookupFlags)0)
							};
						} catch {}

						core = new Core (view);
						core.RegisterServer ();
						
						
						empty = view || Core.Database.Empty;
						control = core;
					}
				} catch (System.Exception e) {
					System.Console.WriteLine ("XXXXX{1}{0}{1}XXXXX", e, Environment.NewLine);
					control = null;

					if (core != null)
						core.UnregisterServer ();
				}
				if (control == null) {
					System.Console.WriteLine ("Can't get a connection to the dbus. Trying again...");
					if (++ retry_count > 5) {
						System.Console.WriteLine ("Sorry, couldn't start F-Spot");
						return 1;
					}
				}
			}
			
			
			UriList list = new UriList ();

			if (shutdown) {
				try {
					control.Shutdown ();
				} catch (System.Exception) {
					// trap errors
				}
				System.Environment.Exit (0);
			}

			if (import_uri != null) {
				control.Import (import_uri);
			}

			if (view) {
				foreach (string s in uris)
					list.AddUnknown (s);
				if (list.Count == 0) {
					Help ();
					return 1;
				}
				control.View (list.ToString ());
			}
			
			if (empty && import_uri == null && !view)
				control.Import (null);
			
			if (import_uri != null || !view) {
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
		return 0;
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
		} catch (DllNotFoundException) {
			/* noop */
		} catch (EntryPointNotFoundException) {
		    	/* noop */
		}
	}
}

}
