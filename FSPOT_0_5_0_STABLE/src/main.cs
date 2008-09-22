using Gtk;
using Gnome;
using System;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Mono.Addins;
using Mono.Addins.Setup;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Extensions;

namespace FSpot 
{
public class Driver {
	static void Version ()
	{
		Console.WriteLine (
			"F-Spot  {0} - (c)2003-2008, Novell Inc" + Environment.NewLine +
			"Personal photo management for the GNOME Desktop" + Environment.NewLine, 
			FSpot.Defines.VERSION);
	}

	static void Versions ()
	{
	    Version ();
            Console.WriteLine (".NET Version: " + Environment.Version.ToString());
            Console.WriteLine (String.Format("{0}Assembly Version Information:", Environment.NewLine));
            
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
		    AssemblyName name = asm.GetName();
                    Console.WriteLine ("\t" + name.Name + " (" + name.Version.ToString () + ")");
		}	
	}

	static void Help ()
	{
		Version ();
		Console.WriteLine ();
		Console.WriteLine (
			"Usage: f-spot [options] " + Environment.NewLine +
			"Options:" + Environment.NewLine +
		  	"-b -basedir PARAM   path to the photo database folder" + Environment.NewLine +
		    	"-? -help -usage     Show this help list" + Environment.NewLine +
			"-i -import PARAM    import from the given uri" + Environment.NewLine +
			"-p -photodir PARAM  default import folder" + Environment.NewLine +
			"-shutdown           shutdown a running f-spot instance" + Environment.NewLine +
			"-slideshow          display a slideshow" + Environment.NewLine +
			"-V -version         Display version and licensing information" + Environment.NewLine +
			"-versions           Display version and dependencies informations" + Environment.NewLine +
			"-v -view            view file(s) or directory(ies)");
	}

	static int Main (string [] args)
	{
		bool empty = false;
		Program program = null;
		ICore control = null;
		List<string> uris = new List<string> ();
		Unix.SetProcessName (Defines.PACKAGE);

		// Options and Option parsing
		bool shutdown = false;
		bool view = false;
		bool slideshow = false;
		string import_uri = null;
		
		program = new Program (Defines.PACKAGE, 
				       Defines.VERSION, 
				       Modules.UI, args);		
		
		FSpot.Global.PhotoDirectory = Preferences.Get<string> (Preferences.STORAGE_PATH);

		for (int i = 0; i < args.Length && !shutdown; i++) {
			switch (args [i]) {
			case "-h": case "-?": case "-help": case "--help": case "-usage":
				Help ();
				return 0;
			
			case "-shutdown": case "--shutdown":
				Log.Information ("Shutting down existing F-Spot server...");
				shutdown = true;
				break;

			case "-b": case "-basedir": case "--basedir":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Log.Error ("f-spot: -basedir option takes one argument");
					return 1;
				}
				FSpot.Global.BaseDirectory = args [++i];
				Log.InformationFormat ("BaseDirectory is now {0}", FSpot.Global.BaseDirectory);
				break;

			case "-p": case "-photodir": case "--photodir":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Log.Error ("f-spot: -photodir option takes one argument");
					return 1;
				}
				FSpot.Global.PhotoDirectory = System.IO.Path.GetFullPath (args [++i]);
				Log.InformationFormat ("PhotoDirectory is now {0}", FSpot.Global.PhotoDirectory);
				break;

			case "-i": case "-import": case "--import":
				if (i+1 == args.Length) {
					Log.Error ("f-spot: -import option takes one argument");
					return 1;
				}
				import_uri = args [++i];
				break;
			
			case "-slideshow": case "--slideshow":
				slideshow = true;
				break;

			case "-v": case "-view": case "--view":
				if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
					Log.Error ("f-spot: -view option takes (at least) one argument");
					return 1;
				}
				view = true;
				while (!(i+1 == args.Length) && !args[i+1].StartsWith ("-"))
					uris.Add (args [++i]);
//				if (!System.IO.Directory.Exists (args[i+1]) && !System.IO.File.Exists (args[i+1])) {
//					Log.Error ("f-spot: -view argument must be an existing file or directory");
//					return 1;
//				}
				break;

			case "-versions": case "--versions":
				Versions ();
				return 0;
			
			case "-V": case "-version": case "--version":
				Version ();
				return 0;
			
			case "--debug":
				Log.Debugging = true;
				// Debug GdkPixbuf critical warnings
				GLib.LogFunc logFunc = new GLib.LogFunc (GLib.Log.PrintTraceLogFunction);
				GLib.Log.SetLogHandler ("GdkPixbuf", GLib.LogLevelFlags.Critical, logFunc);
		
				// Debug Gtk critical warnings
				GLib.Log.SetLogHandler ("Gtk", GLib.LogLevelFlags.Critical, logFunc);

				break;
			case "--uninstalled": case "--gdb":
				break;
			default:
				if (args [i].StartsWith ("--profile"))
					break;
				if (args [i].StartsWith ("--trace"))
					break;
				Log.DebugFormat ("Unparsed argument >>{0}<<", args [i]);
				Help ();
				return 1;
			}
		}

		// Validate command line options
		if ( (import_uri != null && (view || shutdown || slideshow)) || 
		     (view && (shutdown || slideshow)) ||
		     (shutdown && slideshow) ) {
			Log.Error ("Can't mix -import, -view, -shutdown or -slideshow");
			return 1;
		}

		if (slideshow == true) {
			Catalog.Init ("f-spot", Defines.LOCALE_DIR);
				
			Core core = new Core ();
			core.ShowSlides (null);
			program.Run ();
			Log.Debug ("done");
			return 0;
		}
		
		try {

			uint timer = Log.InformationTimerStart ("Initializing DBus");
			try {
				NDesk.DBus.BusG.Init();
			} catch (Exception e) {
				throw new ApplicationException ("F-Spot cannot find the Dbus session bus.  Make sure dbus is configured properly or start a new session for f-spot using \"dbus-launch f-spot\"", e);
			}
			Log.DebugTimerPrint (timer, "DBusInitialization took {0}");
			uint ma_timer = Log.InformationTimerStart ("Initializing Mono.Addins");
			AddinManager.Initialize (FSpot.Global.BaseDirectory);
			AddinManager.Registry.Update (null);
			SetupService setupService = new SetupService (AddinManager.Registry);
			string maj_version = String.Join (".", Defines.VERSION.Split ('.'), 0, 3);
			foreach (AddinRepository repo in setupService.Repositories.GetRepositories ())
				if (repo.Url.StartsWith ("http://addins.f-spot.org/") && !repo.Url.StartsWith ("http://addins.f-spot.org/" + maj_version)) {
					Log.InformationFormat ("Unregistering {0}", repo.Url);
					setupService.Repositories.RemoveRepository (repo.Url);
				}
			setupService.Repositories.RegisterRepository (null, "http://addins.f-spot.org/" + maj_version, false);
			Log.DebugTimerPrint (ma_timer, "Mono.Addins Initialization took {0}");
			
		

			bool create = true;
			int retry_count = 0;
			while (control == null) {
				try {
					control = Core.FindInstance ();
					Log.InformationFormat ("Found active FSpot server: {0}", control);
					program = null;
				} catch (System.Exception) { 
					if (!shutdown)
						Log.Information ("Starting new FSpot server");
				}
				
				Core core = null;
				try {
					if (control == null && create) {
						create = false;
						Gnome.Vfs.Vfs.Initialize ();

						if (File.Exists (Preferences.Get<string> (Preferences.GTK_RC))) {
#if GTK_2_12_2
							if (!File.Exists (Path.Combine (Global.BaseDirectory, "gtkrc")))
								(File.Create (Path.Combine (Global.BaseDirectory, "gtkrc"))).Dispose ();
							Gtk.Rc.AddDefaultFile (Path.Combine (Global.BaseDirectory, "gtkrc"));
							Global.DefaultRcFiles = Gtk.Rc.DefaultFiles;
#endif
							Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GTK_RC));
						}
						
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

					// if there is a problem with the DB, so is no way we can survive
					if (e is DbException) {
						throw;
					}
				}
				if (control == null) {
					Log.Warning ("Can't get a connection to the dbus. Trying again...");
					if (++ retry_count > 5) {
						Log.Error ("Sorry, couldn't start F-Spot");
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
				foreach (ServiceNode service in AddinManager.GetExtensionNodes ("/FSpot/Services")) {
					service.Initialize ();
					service.Start ();
				}
			}			

#if GSD_2_24
			Log.Information ("Hack for gnome-settings-daemon engaged");
			int max_age, max_size;
			if (Preferences.TryGet<int> (Preferences.GSD_THUMBS_MAX_AGE, out max_age)) {
				if (max_age < 0)
					Log.Debug ("maximum_age check already disabled, good");
				else if (max_age == 0)
					Log.Warning ("maximum_age is 0 (tin-hat mode), not overriding");
				else if (max_age < 180) {
					Log.Debug ("Setting maximum_age to a saner value");
					Preferences.Set (Preferences.GSD_THUMBS_MAX_AGE, 180);
				}
			}

			if (Preferences.TryGet<int> (Preferences.GSD_THUMBS_MAX_SIZE, out max_size)) {
				int count = Core.Database.Photos.Count ("photos");
				// average thumbs are taking 70K, so this will push the threshold
				//if f-spot takes more than 70% of the thumbs space
				int size = count / 10;
				if (max_size < 0)
					Log.Debug ("maximum_size check already disabled, good");
				else if (max_size == 0)
					Log.Warning ("maximum_size is 0 (tin-hat mode), not overriding");
				else if (max_size < size) {
					Log.DebugFormat ("Setting maximum_size to a saner value ({0}MB), according to your db size", size);
					Preferences.Set (Preferences.GSD_THUMBS_MAX_SIZE, size);
				}
			}

#endif
			if (program != null)
				program.Run ();
			
			Log.Information ("exiting");
		} catch (System.Exception e) {
			Log.Exception (e);
			ExceptionDialog dlg = new ExceptionDialog(e);
			dlg.Run();
			dlg.Destroy();
			System.Environment.Exit(1);
		}
		return 0;
	}
}
}
