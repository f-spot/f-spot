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
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;

using FSpot.Settings;
using FSpot.Utils;

using Hyena;
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

		static void ShowHelp (CommandLineOptions options)
		{
			Console.WriteLine ("Usage: f-spot [options...] [files|URIs...]");
			Console.WriteLine ();

			if (options.HelpAll) {
				Console.WriteLine (CommandLineHelp.Everything ());
				return;
			}

			if (options.Help)
				Console.WriteLine (CommandLineHelp.Section ("help"));

			if (options.HelpOptions)
				Console.WriteLine (CommandLineHelp.Section ("options"));

			if (options.InvalidHelpArguments.Count > 0) {
				Console.WriteLine ("The following help arguments are invalid: " +
					string.Join (", ", options.InvalidHelpArguments.Select (e => $"--{e}")));
			}
		}

		static int Main (string[] args)
		{
			Logger.CreateLogger ();

			if (Environment.Is64BitProcess)
				throw new ApplicationException ("GtkSharp does not support running 64bit");

			ApplicationContext.ApplicationName = "F-Spot";
			ApplicationContext.TrySetProcessName (FSpotConfiguration.Package);

			var mainContext = new GLibSynchronizationContext ();
			SynchronizationContext.SetSynchronizationContext (mainContext);
			ThreadAssist.InitializeMainThread ();
			// ProxyToMain and the synchronization context marshal to the same main loop,
			// so route one through the other rather than keeping GLib.Idle.Add alive as a
			// second implementation. At idle priority, which is where GLib.Idle.Add put
			// these and where the bulk UI updates ProxyToMain carries belong. The handler
			// is only reached from off the main thread -- ProxyToMain invokes directly
			// when it is already on it -- which is exactly when posting is wanted.
			ThreadAssist.ProxyToMainHandler = handler => mainContext.PostAtIdlePriority (_ => handler (), null);

			GLib.GType.Init ();

			FSpotConfiguration.PhotoUri = new SafeUri (Preferences.Get<string> (Preferences.StoragePath));

			var options = CommandLineOptions.Parse (args);
			CommandLineOptions.Current = options;
			// Hyena's ApplicationContext.Debugging used to read the parser itself; it is
			// told instead, so nothing below the client depends on how args are parsed.
			// DebuggingRequested rather than the two flags, so BANSHEE_DEBUG still counts:
			// assigning here means a lazy fallback in the getter could never evaluate.
			ApplicationContext.Debugging = options.DebuggingRequested;

			if (options.WantsAnyHelp) {
				ShowHelp (options);
				return 0;
			}

			if (options.Version) {
				ShowVersion ();
				return 0;
			}

			if (options.Versions) {
				ShowAssemblyVersions ();
				return 0;
			}

			if (options.Shutdown)
				Logger.Log.Information ("Shutting down existing F-Spot server...");

			if (options.Slideshow)
				Logger.Log.Information ("Running F-Spot in slideshow mode.");

			if (options.BaseDir != null) {
				if (!string.IsNullOrEmpty (options.BaseDir)) {
					FSpotConfiguration.BaseDirectory = options.BaseDir;
					Logger.Log.Information ($"BaseDirectory is now {options.BaseDir}");
				} else {
					Logger.Log.Error ("f-spot: -basedir option takes one argument");
					return 1;
				}
			}

			if (options.PhotoDir != null) {
				if (!string.IsNullOrEmpty (options.PhotoDir)) {
					FSpotConfiguration.PhotoUri = new SafeUri (options.PhotoDir);
					Logger.Log.Information ($"PhotoDirectory is now {options.PhotoDir}");
				} else {
					Logger.Log.Error ("f-spot: -photodir option takes one argument");
					return 1;
				}
			}

			if (options.Debug) {
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
			bool import = options.Import != null;
			if ((import && (options.View || options.Shutdown || options.Slideshow)) ||
				(options.View && (options.Shutdown || options.Slideshow)) ||
				(options.Shutdown && options.Slideshow)) {
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

				GtkSettings.DefaultRcFiles = Gtk.Rc.DefaultFiles;
				Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GtkRc));
				Gtk.Rc.ReparseAllForSettings (Gtk.Settings.Default, true);
			}

			try {
				Gtk.Window.DefaultIconList = new[] {
					GtkUtil.TryLoadIcon (GtkSettings.IconTheme, "FSpot", 16, 0),
					GtkUtil.TryLoadIcon (GtkSettings.IconTheme, "FSpot", 22, 0),
					GtkUtil.TryLoadIcon (GtkSettings.IconTheme, "FSpot", 32, 0),
					GtkUtil.TryLoadIcon (GtkSettings.IconTheme, "FSpot", 48, 0)
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
			var options = CommandLineOptions.Current;

			if (options.Slideshow)
				App.Instance.Slideshow (null);
			else if (options.Shutdown)
				App.Instance.Shutdown ();
			else if (options.View) {
				if (options.Files.Count == 0) {
					Logger.Log.Error ("f-spot: -view option takes at least one argument");
					Environment.Exit (1);
				}

				var list = new UriList ();

				foreach (var f in options.Files)
					list.AddUnknown (f);

				if (list.Count == 0) {
					ShowHelp (options);
					Environment.Exit (1);
				}

				App.Instance.View (list);
			} else if (options.Import != null) {
				string dir = options.Import;

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
	}
}
