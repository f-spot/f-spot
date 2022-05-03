//
// Main.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Paul Lange <palango@gmx.de>
//   Evan Briones <erbriones@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2010 Paul Lange
// Copyright (C) 2010 Evan Briones
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

using FSpot.Settings;
using FSpot.Utils;

using Hyena;
using Hyena.CommandLine;
using Hyena.Gui;

using Mono.Addins;
using Mono.Addins.Setup;

using SerilogTimings;

namespace FSpot
{
	public static class Driver
	{
		static void ShowVersion ()
		{
			Console.WriteLine ($"F-Spot {FSpotConfiguration.Version}");
			Console.WriteLine ("http://f-spot.org");
			Console.WriteLine ("\t(c)2003-2009, Novell Inc");
			Console.WriteLine ("\t(c)2009 Stephane Delcroix");
			Console.WriteLine ("Personal photo management for the GNOME Desktop");
		}

		static void ShowAssemblyVersions ()
		{
			ShowVersion ();
			Console.WriteLine ();
			Console.WriteLine ("Mono/.NET Version: " + Environment.Version);
			Console.WriteLine ($"{Environment.NewLine}Assembly Version Information:");

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				AssemblyName name = asm.GetName ();
				Console.WriteLine ($"\t{name.Name} ({name.Version})");
			}
		}

		static void ShowHelp ()
		{
			Console.WriteLine ("Usage: f-spot [options...] [files|URIs...]");
			Console.WriteLine ();

			var commands = new Hyena.CommandLine.Layout (
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
			foreach (var argument in ApplicationContext.CommandLine.Arguments) {
				switch (argument.Key) {
				case "help": Console.WriteLine (commands.ToString ("help")); break;
				case "help-options": Console.WriteLine (commands.ToString ("options")); break;
				default:
					if (argument.Key.StartsWith ("help", StringComparison.OrdinalIgnoreCase)) {
						(errors ??= new List<string> ()).Add (argument.Key);
					}
					break;
				}
			}

			if (errors != null)
				Console.WriteLine (commands.LayoutLine ($"The following help arguments are invalid: {Hyena.Collections.CollectionExtensions.Join (errors, "--", null, ", ")}"));
		}

		static string[] FixArgs (IReadOnlyList<string> args)
		{
			// Makes sure command line arguments are parsed backwards compatible.
			var outargs = new List<string> ();
			for (int i = 0; i < args.Count; i++) {
				switch (args[i]) {
				case "-h":
				case "-help":
				case "-usage":
					outargs.Add ("--help");
					break;
				case "-V":
				case "-version":
					outargs.Add ("--version");
					break;
				case "-versions":
					outargs.Add ("--versions");
					break;
				case "-shutdown":
					outargs.Add ("--shutdown");
					break;
				case "-b":
				case "-basedir":
				case "--basedir":
					outargs.Add ("--basedir=" + (i + 1 == args.Count ? string.Empty : args[++i]));
					break;
				case "-p":
				case "-photodir":
				case "--photodir":
					outargs.Add ("--photodir=" + (i + 1 == args.Count ? string.Empty : args[++i]));
					break;
				case "-i":
				case "-import":
				case "--import":
					outargs.Add ("--import=" + (i + 1 == args.Count ? string.Empty : args[++i]));
					break;
				case "-v":
				case "-view":
					outargs.Add ("--view");
					break;
				case "-slideshow":
					outargs.Add ("--slideshow");
					break;
				default:
					outargs.Add (args[i]);
					break;
				}
			}
			return outargs.ToArray ();
		}

		static int Main (string[] args)
		{
			Logger.CreateLogger ();

			if (Environment.Is64BitProcess)
				throw new ApplicationException ("GtkSharp does not support running 64bit");

			args = FixArgs (args);

			ApplicationContext.ApplicationName = "F-Spot";
			ApplicationContext.TrySetProcessName (FSpotConfiguration.Package);

			SynchronizationContext.SetSynchronizationContext (new GtkSynchronizationContext ());
			ThreadAssist.InitializeMainThread ();
			ThreadAssist.ProxyToMainHandler = RunIdle;

			// Options and Option parsing
			bool shutdown = false;
			bool view = false;
			bool slideshow = false;
			bool import = false;

			GLib.GType.Init ();

			FSpotConfiguration.PhotoUri = new SafeUri (Preferences.Get<string> (Preferences.StoragePath));

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
				Logger.Log.Information ("Shutting down existing F-Spot server...");
				shutdown = true;
			}

			if (ApplicationContext.CommandLine.Contains ("slideshow")) {
				Logger.Log.Information ("Running F-Spot in slideshow mode.");
				slideshow = true;
			}

			if (ApplicationContext.CommandLine.Contains ("basedir")) {
				string dir = ApplicationContext.CommandLine["basedir"];

				if (!string.IsNullOrEmpty (dir)) {
					FSpotConfiguration.BaseDirectory = dir;
					Logger.Log.Information ($"BaseDirectory is now {dir}");
				} else {
					Logger.Log.Error ("f-spot: -basedir option takes one argument");
					return 1;
				}
			}

			if (ApplicationContext.CommandLine.Contains ("photodir")) {
				string dir = ApplicationContext.CommandLine["photodir"];

				if (!string.IsNullOrEmpty (dir)) {
					FSpotConfiguration.PhotoUri = new SafeUri (dir);
					Logger.Log.Information ($"PhotoDirectory is now {dir}");
				} else {
					Logger.Log.Error ("f-spot: -photodir option takes one argument");
					return 1;
				}
			}

			if (ApplicationContext.CommandLine.Contains ("import"))
				import = true;

			if (ApplicationContext.CommandLine.Contains ("view"))
				view = true;

			if (ApplicationContext.CommandLine.Contains ("debug")) {
				Logger.SetLevel (Serilog.Events.LogEventLevel.Debug);
				// Debug GdkPixbuf critical warnings
				var logFunc = new GLib.LogFunc (GLib.Log.PrintTraceLogFunction);
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
			if ((import && (view || shutdown || slideshow)) ||
				(view && (shutdown || slideshow)) ||
				(shutdown && slideshow)) {
				Logger.Log.Error ("Can't mix -import, -view, -shutdown or -slideshow");
				return 1;
			}

			InitializeAddins ();

			// Gtk initialization
			Gtk.Application.Init (FSpotConfiguration.Package, ref args);
			// Maybe we'll add this at a future date
			//Xwt.Application.InitializeAsGuest (Xwt.ToolkitType.Gtk);

			// init web proxy globally
			// FIXME, Reenable this at some point?
			//FSpotWebProxy.Init ();

			if (File.Exists (Preferences.Get<string> (Preferences.GtkRc))) {
				if (File.Exists (Path.Combine (FSpotConfiguration.BaseDirectory, "gtkrc")))
					Gtk.Rc.AddDefaultFile (Path.Combine (FSpotConfiguration.BaseDirectory, "gtkrc"));

				FSpotConfiguration.DefaultRcFiles = Gtk.Rc.DefaultFiles;
				Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GtkRc));
				Gtk.Rc.ReparseAllForSettings (Gtk.Settings.Default, true);
			}

			try {
				Gtk.Window.DefaultIconList = new[] {
					GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpot", 16, 0),
					GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpot", 22, 0),
					GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpot", 32, 0),
					GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "FSpot", 48, 0)
				};
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Loading default f-spot icons");
			}

			GLib.ExceptionManager.UnhandledException += exceptionArgs => {
				Console.WriteLine ("Unhandled exception handler:");
				if (exceptionArgs.ExceptionObject is Exception exception) {
					Console.WriteLine ($"Message: {exception.Message}");
					Console.WriteLine ($"Stack trace: {exception.StackTrace}");
				} else {
					Console.WriteLine ($"Unknown exception type: {exceptionArgs.ExceptionObject.GetType ()}");
				}
			};

			CleanRoomStartup.Startup (Startup);

			// Running threads are preventing the application from quitting
			// we force it for now until this is fixed
			Environment.Exit (0);
			return 0;
		}

		static void InitializeAddins ()
		{
			using var op = Operation.Begin ($"Mono.Addins Initialization");
			try {
				UpdatePlugins ();
			} catch (Exception) {
				Logger.Log.Debug ("Failed to initialize plugins, will remove addin-db and try again.");
				ResetPluginDb ();
			}

			var setupService = new SetupService (AddinManager.Registry);
			foreach (AddinRepository repo in setupService.Repositories.GetRepositories ()) {
				if (repo.Url.StartsWith ("http://addins.f-spot.org/", StringComparison.OrdinalIgnoreCase)) {
					Logger.Log.Information ($"Unregistering {repo.Url}");
					setupService.Repositories.RemoveRepository (repo.Url);
				}
			}

			op.Complete ();
		}

		static void UpdatePlugins ()
		{
			AddinManager.Initialize (FSpotConfiguration.BaseDirectory);
			AddinManager.Registry.Update (null);
		}

		static void ResetPluginDb ()
		{
			// FIXME, test this.
			// Nuke addin-db
			var directory = new DirectoryInfo (new SafeUri (FSpotConfiguration.BaseDirectory));
			foreach (var item in directory.EnumerateDirectories ()) {
				if (item.Name.StartsWith ("addin-db-", StringComparison.OrdinalIgnoreCase))
					item.Delete (true);
			}

			// Try again
			UpdatePlugins ();
		}

		static void Startup ()
		{
			if (ApplicationContext.CommandLine.Contains ("slideshow"))
				App.Instance.Slideshow (null);
			else if (ApplicationContext.CommandLine.Contains ("shutdown"))
				App.Instance.Shutdown ();
			else if (ApplicationContext.CommandLine.Contains ("view")) {
				if (ApplicationContext.CommandLine.Files.Count == 0) {
					Logger.Log.Error ("f-spot: -view option takes at least one argument");
					Environment.Exit (1);
				}

				var list = new UriList ();

				foreach (var f in ApplicationContext.CommandLine.Files)
					list.AddUnknown (f);

				if (list.Count == 0) {
					ShowHelp ();
					Environment.Exit (1);
				}

				App.Instance.View (list);
			} else if (ApplicationContext.CommandLine.Contains ("import")) {
				string dir = ApplicationContext.CommandLine["import"];

				if (string.IsNullOrEmpty (dir)) {
					Logger.Log.Error ("f-spot: -import option takes one argument");
					Environment.Exit (1);
				}

				App.Instance.Import (dir);
			} else
				App.Instance.Organize ();

			//if (!App.Instance.IsRunning)
			try {
				Gtk.Application.Run ();
			} catch (Exception ex) {
				Logger.Log.Error (ex, "");
			}
		}

		public static void RunIdle (InvokeHandler handler)
		{
			GLib.Idle.Add (delegate { handler (); return false; });
		}
	}
}
