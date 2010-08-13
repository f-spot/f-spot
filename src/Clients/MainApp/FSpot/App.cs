/*
 * FSpot.Application.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Stephane Delcroix.
 *
 * This is open source software. See COPYING fro details.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Unique;

using Mono.Unix;

using Hyena;

using FSpot.Core;
using FSpot.Database;

namespace FSpot
{
	public class App : Unique.App
	{
		static object sync_handle = new object ();

#region public API
		static App app;
		public static App Instance {
			get {
				lock (sync_handle) {
					if (app == null)
						app = new App ();
				}
				return app;
			}
		}

		Thread constructing_organizer = null;

		public MainWindow Organizer {
			get {
				lock (sync_handle) {
					if (organizer == null) {
						if (constructing_organizer == Thread.CurrentThread) {
							throw new Exception ("Recursively tried to acquire App.Organizer!");
						}

						constructing_organizer = Thread.CurrentThread;
						organizer = new MainWindow (Database);
						Register (organizer.Window);
					}
				}
				return organizer;
			}
		}

		public Db Database {
			get {
				lock (sync_handle) {
					if (db == null) {
						if (!File.Exists (Global.BaseDirectory))
							Directory.CreateDirectory (Global.BaseDirectory);

						db = new Db ();

						try {
							db.Init (Path.Combine (Global.BaseDirectory, "photos.db"), true);
						} catch (Exception e) {
							new FSpot.UI.Dialog.RepairDbDialog (e, db.Repair (), null);
							db.Init (Path.Combine (Global.BaseDirectory, "photos.db"), true);
						}
					}
				}
				return db;
			}
		}

		public void Import (string path)
		{
			if (IsRunning) {
				var md = new MessageData ();
				md.Text = path;
				SendMessage (Command.Import, md);
				return;
			}
			HandleImport (path);
		}

		public void Organize ()
		{
			if (IsRunning) {
				SendMessage (Command.Organize, null);
				return;
			}
			HandleOrganize ();
		}

		public void Shutdown ()
		{
			if (IsRunning) {
				SendMessage (Command.Shutdown, null);
				return;
			}
			HandleShutdown ();
		}

		public void Slideshow (string tagname)
		{
			if (IsRunning) {
				var md = new MessageData ();
				md.Text = tagname ?? String.Empty;
				SendMessage (Command.Slideshow, md);

				return;
			}
			HandleSlideshow (tagname);
		}

		public void View (SafeUri uri)
		{
			View (new SafeUri[] {uri});
		}

		public void View (IEnumerable<SafeUri> uris)
		{
			var uri_s = from uri in uris select uri.ToString ();
			View (uri_s);
		}

		public void View (string uri)
		{
			View (new string[] {uri});
		}

		public void View (IEnumerable<string> uris)
		{
			if (IsRunning) {
				var md = new MessageData ();
				md.Uris = uris.ToArray ();
				SendMessage (Command.View, md);
				return;
			}
			HandleView (uris.ToArray());
		}
#endregion

#region private ctor and stuffs
		enum Command {
			Invalid = 0,
			Import,
			View,
			Organize,
			Shutdown,
			Version,
			Slideshow,
		}

		List<Gtk.Window> toplevels;
		MainWindow organizer;
		Db db;

		App (): base ("org.gnome.FSpot.Core", null,
				  "Import", Command.Import,
				  "View", Command.View,
				  "Organize", Command.Organize,
				  "Shutdown", Command.Shutdown,
				  "Slideshow", Command.Slideshow)
		{
			toplevels = new List<Gtk.Window> ();
			if (IsRunning) {
				Log.Information ("Found active FSpot process");
			} else {
				MessageReceived += HandleMessageReceived;
			}
		}

		void SendMessage (Command command, MessageData md)
		{
			SendMessage ((Unique.Command)command, md);
		}
#endregion

#region Command Handlers

		void HandleMessageReceived (object sender, MessageReceivedArgs e)
		{
			switch ((Command)e.Command) {
			case Command.Import:
				HandleImport (e.MessageData.Text);
				e.RetVal = Response.Ok;
				break;
			case Command.Organize:
				HandleOrganize ();
				e.RetVal = Response.Ok;
				break;
			case Command.Shutdown:
				HandleShutdown ();
				e.RetVal = Response.Ok;
				break;
			case Command.Slideshow:
				HandleSlideshow (e.MessageData.Text);
				e.RetVal = Response.Ok;
				break;
			case Command.View:
				HandleView (e.MessageData.Uris);
				e.RetVal = Response.Ok;
				break;
			case Command.Invalid:
			default:
				Log.Debug ("Wrong command received");
				break;
			}
		}

		void HandleImport (string path)
		{
			Organizer.Window.Present ();
			Organizer.ImportFile (path == null ? null : new SafeUri(path));
		}

		void HandleOrganize ()
		{
			if (Database.Empty)
				HandleImport (null);
			else
				Organizer.Window.Present ();
		}

		void HandleShutdown ()
		{
			try {
				App.Instance.Organizer.Close ();
			} catch {
				System.Environment.Exit (0);
			}
		}

		//FIXME move all this in a standalone class
		void HandleSlideshow (string tagname)
		{
			Tag tag;
			FSpot.Widgets.SlideShow slideshow = null;

			if (!String.IsNullOrEmpty (tagname))
				tag = Database.Tags.GetTagByName (tagname);
			else
				tag = Database.Tags.GetTagById (Preferences.Get<int> (Preferences.SCREENSAVER_TAG));

			IPhoto[] photos;
			if (tag != null)
				photos = Database.Photos.Query (new Tag[] {tag});
			else if (Preferences.Get<int> (Preferences.SCREENSAVER_TAG) == 0)
				photos = Database.Photos.Query (new Tag [] {});
			else
				photos = new IPhoto [0];

			// Minimum delay 1 second; default is 4s
			var delay = Math.Max (1.0, Preferences.Get<double> (Preferences.SCREENSAVER_DELAY));

			var window = new XScreenSaverSlide ();
			window.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			window.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));

			if (photos.Length > 0) {
				Array.Sort (photos, new IPhotoComparer.RandomSort ());
				slideshow = new FSpot.Widgets.SlideShow (new BrowsablePointer (new PhotoList (photos), 0), (uint)(delay * 1000), true);
				slideshow.Transition = new FSpot.Transitions.DissolveTransition ();
				window.Add (slideshow);
			} else {
				Gtk.HBox outer = new Gtk.HBox ();
				Gtk.HBox hbox = new Gtk.HBox ();
				Gtk.VBox vbox = new Gtk.VBox ();

				outer.PackStart (new Gtk.Label (String.Empty));
				outer.PackStart (vbox, false, false, 0);
				vbox.PackStart (new Gtk.Label (String.Empty));
				vbox.PackStart (hbox, false, false, 0);
				hbox.PackStart (new Gtk.Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Dialog),
						false, false, 0);
				outer.PackStart (new Gtk.Label (String.Empty));

				string msg;
				string long_msg;

				if (tag != null) {
					msg = String.Format (Catalog.GetString ("No photos matching {0} found"), tag.Name);
					long_msg = String.Format (Catalog.GetString ("The tag \"{0}\" is not applied to any photos. Try adding\n" +
										     "the tag to some photos or selecting a different tag in the\n" +
										     "F-Spot preference dialog."), tag.Name);
				} else {
					msg = Catalog.GetString ("Search returned no results");
					long_msg = Catalog.GetString ("The tag F-Spot is looking for does not exist. Try\n" +
								      "selecting a different tag in the F-Spot preference\n" +
								      "dialog.");
				}

				Gtk.Label label = new Gtk.Label (msg);
				hbox.PackStart (label, false, false, 0);

				Gtk.Label long_label = new Gtk.Label (long_msg);
				long_label.Markup  = String.Format ("<small>{0}</small>", long_msg);

				vbox.PackStart (long_label, false, false, 0);
				vbox.PackStart (new Gtk.Label (String.Empty));

				window.Add (outer);
				label.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
				label.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
				long_label.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
				long_label.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			}
			window.ShowAll ();

			Register (window);
			GLib.Idle.Add (delegate {
				if (slideshow != null)
					slideshow.Start ();
				return false;
			});
		}

		void HandleView (string[] uris)
		{
			List<SafeUri> ul = new List<SafeUri> ();
			foreach (var u in uris)
				ul.Add (new SafeUri (u, true));
			try {
				Register (new FSpot.SingleView (ul.ToArray ()).Window);
			} catch (System.Exception e) {
				Log.Exception (e);
				Log.Debug ("no real valid path to view from");
			}
		}

#endregion

#region Track toplevel windows
		void Register (Gtk.Window window)
		{
			toplevels.Add (window);
			window.Destroyed += HandleDestroyed;
		}

		void HandleDestroyed (object sender, EventArgs e)
		{
			toplevels.Remove (sender as Gtk.Window);
			if (toplevels.Count == 0) {
				Log.Information ("Exiting...");
				Banshee.Kernel.Scheduler.Dispose ();
				Database.Dispose ();
				ImageLoaderThread.CleanAll ();
				Gtk.Application.Quit ();
				System.Environment.Exit (0);
			}
			if (organizer != null && organizer.Window == sender)
				organizer = null;
		}
#endregion
	}
}
