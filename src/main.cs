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
using Hyena;
using Hyena.CommandLine;

namespace FSpot
{
	public static class Driver
	{
		private static void ShowVersion ()
		{
			Console.WriteLine ("F-Spot {0}", FSpot.Defines.VERSION);
			Console.WriteLine ("http://f-spot.org");
			Console.WriteLine ("\t(c)2003-2009, Novell Inc");
			Console.WriteLine ("\t(c)2009 Stephane Delcroix");
			Console.WriteLine("Personal photo management for the GNOME Desktop");
		}

		private static void ShowAssemblyVersions ()
		{
			ShowVersion ();
			Console.WriteLine ();
			Console.WriteLine ("Mono/.NET Version: " + Environment.Version.ToString ());
			Console.WriteLine (String.Format ("{0}Assembly Version Information:", Environment.NewLine));

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ())
			{
				AssemblyName name = asm.GetName ();
				Console.WriteLine ("\t" + name.Name + " (" + name.Version.ToString () + ")");
			}
		}

		private static void ShowHelp ()
		{
			Console.WriteLine ("Usage: f-spot [options...] [files|URIs...]");
			Console.WriteLine ();

			Hyena.CommandLine.Layout commands = new Hyena.CommandLine.Layout (
				new LayoutGroup ("help", "Help Options",
					new LayoutOption ("help", "Show this help"),
					new LayoutOption ("help-options", "Show command line options"),
					new LayoutOption ("help-all", "Show all options"),
					new LayoutOption ("version", "Show version information"),
					new LayoutOption ("versions", "Show detailed version information")),
				new LayoutGroup ("options", "General options",
					new LayoutOption ("basedir=DIR", "Path to the photo database folder"),
					new LayoutOption ("import=URI", "Import from the given uri"),
					new LayoutOption ("photodir=DIR", "Default import folder"),
					new LayoutOption ("view ITEM", "View file(s) or directories"),
					new LayoutOption ("shutdown", "Shut down a running instance of F-Spot"),
					new LayoutOption ("slideshow", "Display a slideshow"),
					new LayoutOption ("debug", "Run in debug mode")));

			if (ApplicationContext.CommandLine.Contains ("help-all")) {
				Console.WriteLine (commands);
				return;
			}

			List<string> errors = null;
			foreach (KeyValuePair<string, string> argument in ApplicationContext.CommandLine.Arguments) {
				switch (argument.Key) {
					case "help": Console.WriteLine (commands.ToString ("help")); break;
					case "help-options": Console.WriteLine (commands.ToString ("options")); break;
					default:
						if (argument.Key.StartsWith ("help")) {
							(errors ?? (errors = new List<string> ())).Add (argument.Key);
						}
						break;
				}
			}

			if (errors != null) {
				Console.WriteLine (commands.LayoutLine (String.Format (
					"The following help arguments are invalid: {0}",
					Hyena.Collections.CollectionExtensions.Join (errors, "--", null, ", "))));
			}
		}

		static string [] FixArgs (string [] args)
		{
			// Makes sure command line arguments are parsed backwards compatible.
			var outargs = new List<string> ();
			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
					case "-h": case "-help": case "-usage":
						outargs.Add ("--help");
						break;
					case "-V": case "-version":
						outargs.Add ("--version");
						break;
					case "-versions":
						outargs.Add ("--versions");
						break;
					case "-shutdown":
						outargs.Add ("--shutdown");
						break;
					case "-b": case "-basedir": case "--basedir":
						outargs.Add ("--basedir=" + (i + 1 == args.Length ? String.Empty : args [++i]));
						break;
					case "-p": case "-photodir": case "--photodir":
						outargs.Add ("--photodir=" + (i + 1 == args.Length ? String.Empty : args [++i]));
						break;
					case "-i": case "-import": case "--import":
						outargs.Add ("--import=" + (i + 1 == args.Length ? String.Empty : args [++i]));
						break;
					case "-v": case "-view":
						outargs.Add ("--view");
						break;
					case "-slideshow":
						outargs.Add ("--slideshow");
						break;
					default:
						outargs.Add (args [i]);
						break;
				}
			}
			return outargs.ToArray ();
		}

		static int Main (string [] args)
		{
			args = FixArgs (args);

			Unix.SetProcessName (Defines.PACKAGE);

			ThreadAssist.InitializeMainThread ();
			ThreadAssist.ProxyToMainHandler = RunIdle;
			XdgThumbnailSpec.DefaultLoader = (uri) => {
				using (var file = ImageFile.Create (uri))
					return file.Load ();
			};
			// Options and Option parsing
			bool shutdown = false;
			bool view = false;
			bool slideshow = false;
			string import_uri = null;

			GLib.GType.Init ();
			Catalog.Init ("f-spot", Defines.LOCALE_DIR);
			
			FSpot.Global.PhotoUri = new SafeUri (Preferences.Get<string> (Preferences.STORAGE_PATH));

			ApplicationContext.CommandLine = new CommandLineParser (args, 0);

			if (ApplicationContext.CommandLine.ContainsStart ("help")) {
				ShowHelp ();
				return 0;
			}

			if (ApplicationContext.CommandLine.Contains ("version")) {
				ShowVersion ();
				return 0;
			}

			if (ApplicationContext.CommandLine.Contains ("versions")) {
				ShowAssemblyVersions ();
				return 0;
			}

			if (ApplicationContext.CommandLine.Contains ("shutdown")) {
				Log.Information ("Shutting down existing F-Spot server...");
				shutdown = true;
			}

			if (ApplicationContext.CommandLine.Contains ("slideshow")) {
				Log.Information ("Running F-Spot in slideshow mode.");
				slideshow = true;
			}

			if (ApplicationContext.CommandLine.Contains ("basedir")) {
				string dir = ApplicationContext.CommandLine ["basedir"];

				if (!string.IsNullOrEmpty (dir))
				{
					FSpot.Global.BaseDirectory = dir;
					Log.InformationFormat ("BaseDirectory is now {0}", dir);
				} else {
					Log.Error ("f-spot: -basedir option takes one argument");
					return 1;
				}
			}

			if (ApplicationContext.CommandLine.Contains ("photodir")) {
				string dir = ApplicationContext.CommandLine ["photodir"];

				if (!string.IsNullOrEmpty (dir))
				{
					FSpot.Global.PhotoUri = new SafeUri (dir);
					Log.InformationFormat ("PhotoDirectory is now {0}", dir);
				} else {
					Log.Error ("f-spot: -photodir option takes one argument");
					return 1;
				}
			}

			if (ApplicationContext.CommandLine.Contains ("import")) {
				string dir = ApplicationContext.CommandLine ["import"];

				if (!string.IsNullOrEmpty (dir))
				{
					import_uri = dir;
				} else {
					Log.Error ("f-spot: -import option takes one argument");
					return 1;
				}
			}

			List <string> uris = new List <string> ();
			if (ApplicationContext.CommandLine.Contains ("view")) {
				view = true;
				var items = ApplicationContext.CommandLine.Files;

				if (items.Count > 0)
				{
					uris = new List<string> (items);
				} else {
					Log.Error ("f-spot: -view option takes at least one argument");
					return 1;
				}
			}

			if (ApplicationContext.CommandLine.Contains ("debug")) {
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

				GLib.Log.SetLogHandler ("GLib-GIO", GLib.LogLevelFlags.Critical, logFunc);
			}

			// Validate command line options
			if ( (import_uri != null && (view || shutdown || slideshow)) ||
				(view && (shutdown || slideshow)) ||
				(shutdown && slideshow) )
			{
				Log.Error ("Can't mix -import, -view, -shutdown or -slideshow");
				return 1;
			}

			// Initialize Mono.Addins
			uint timer = Log.InformationTimerStart ("Initializing Mono.Addins");
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
			Log.DebugTimerPrint (timer, "Mono.Addins Initialization took {0}");


			// Gtk initialization
			Gtk.Application.Init (Defines.PACKAGE, ref args);

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
						ShowHelp ();
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

		public static void RunIdle (InvokeHandler handler)
		{
			GLib.Idle.Add (delegate { handler (); return false; });
		}
	}
}
