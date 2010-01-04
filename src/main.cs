using Gtk;
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
	public static class Driver {
		static void Version ()
		{
			Console.WriteLine (
				"F-Spot  {0}" + Environment.NewLine +
				"\t(c)2003-2009, Novell Inc" + Environment.NewLine +
				"\t(c)2009 Stephane Delcroix" + Environment.NewLine +
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
			List<string> uris = new List<string> ();
			Unix.SetProcessName (Defines.PACKAGE);

			// Options and Option parsing
			bool shutdown = false;
			bool view = false;
			bool slideshow = false;
			string import_uri = null;

			GLib.GType.Init ();
			Catalog.Init ("f-spot", Defines.LOCALE_DIR);
			
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
					Log.Information ("BaseDirectory is now {0}", FSpot.Global.BaseDirectory);
					break;

				case "-p": case "-photodir": case "--photodir":
					if (i+1 == args.Length || args[i+1].StartsWith ("-")) {
						Log.Error ("f-spot: -photodir option takes one argument");
						return 1;
					}
					FSpot.Global.PhotoDirectory = System.IO.Path.GetFullPath (args [++i]);
					Log.Information ("PhotoDirectory is now {0}", FSpot.Global.PhotoDirectory);
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

				case "--strace":
					Log.Tracing = true;
					break;

				case "--debug":
					Log.Debugging = true;
					// Debug GdkPixbuf critical warnings
					GLib.LogFunc logFunc = new GLib.LogFunc (GLib.Log.PrintTraceLogFunction);
					GLib.Log.SetLogHandler ("GdkPixbuf", GLib.LogLevelFlags.Critical, logFunc);

					// Debug Gtk critical warnings
					GLib.Log.SetLogHandler ("Gtk", GLib.LogLevelFlags.Critical, logFunc);

					// Debug GLib critical warnings
					GLib.Log.SetLogHandler ("GLib", GLib.LogLevelFlags.Critical, logFunc);

					//Debug GLib-GObject critical warnings
					GLib.Log.SetLogHandler ("GLib-GObject", GLib.LogLevelFlags.Critical, logFunc);

					break;
				case "--uninstalled": case "--gdb": case "--valgrind": case "--sync":
					break;
				default:
					if (args [i].StartsWith ("--profile"))
						break;
					if (args [i].StartsWith ("--trace"))
						break;
					Log.Debug ("Unparsed argument >>{0}<<", args [i]);
					break;
				}
			}

			// Validate command line options
			if ( (import_uri != null && (view || shutdown || slideshow)) ||
			     (view && (shutdown || slideshow)) ||
			     (shutdown && slideshow) ) {
				Log.Error ("Can't mix -import, -view, -shutdown or -slideshow");
				return 1;
			}

			//Initialize Mono.Addins
			uint timer = Log.InformationTimerStart ("Initializing Mono.Addins");
			AddinManager.Initialize (FSpot.Global.BaseDirectory);
			AddinManager.Registry.Update (null);
			SetupService setupService = new SetupService (AddinManager.Registry);
			string maj_version = String.Join (".", Defines.VERSION.Split ('.'), 0, 3);
			foreach (AddinRepository repo in setupService.Repositories.GetRepositories ())
				if (repo.Url.StartsWith ("http://addins.f-spot.org/") && !repo.Url.StartsWith ("http://addins.f-spot.org/" + maj_version)) {
					Log.Information ("Unregistering {0}", repo.Url);
					setupService.Repositories.RemoveRepository (repo.Url);
				}
			setupService.Repositories.RegisterRepository (null, "http://addins.f-spot.org/" + maj_version, false);
			Log.DebugTimerPrint (timer, "Mono.Addins Initialization took {0}");


			//Gtk initialization
			Gtk.Application.Init (Defines.PACKAGE, ref args);
			Gnome.Vfs.Vfs.Initialize ();

			// init web proxy globally
			Platform.WebProxy.Init ();

			if (File.Exists (Preferences.Get<string> (Preferences.GTK_RC))) {
				if (File.Exists (Path.Combine (Global.BaseDirectory, "gtkrc")))
					Gtk.Rc.AddDefaultFile (Path.Combine (Global.BaseDirectory, "gtkrc"));

				Global.DefaultRcFiles = Gtk.Rc.DefaultFiles;
				Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GTK_RC));
				Gtk.Rc.ReparseAllForSettings (Gtk.Settings.Default, true);
			}

			try {
				Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {
					GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 16, (Gtk.IconLookupFlags)0),
					GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 22, (Gtk.IconLookupFlags)0),
					GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 32, (Gtk.IconLookupFlags)0),
					GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 48, (Gtk.IconLookupFlags)0)
				};
			} catch {}

			try {
				if (slideshow == true) {
					App.Instance.Slideshow (null);
				} else if (shutdown) {
					try {
						App.Instance.Shutdown ();
					} catch (System.Exception) { // trap errors
					}
					System.Environment.Exit (0);
				} else if (view) {
					UriList list = new UriList ();
					foreach (string s in uris)
						list.AddUnknown (s);
					if (list.Count == 0) {
						Help ();
						return 1;
					}
					App.Instance.View (list);
				} else if (import_uri != null) {
					App.Instance.Import (import_uri);
				} else {
					App.Instance.Organize ();
				}
	
				if (App.Instance.IsRunning)
					return 0;
				Gtk.Application.Run ();
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
