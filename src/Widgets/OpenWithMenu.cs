/*
 * FSpot.Widgets.OpenWithMenu.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 * 	Loz  <gnome2@flower.powernet.co.uk>
 * 	Gabriel Burt  <gabriel.burt@gmail.com>
 * 	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;
#if GTK_SHARP_2_12_6
using GLib;
#endif

using Mono.Unix;

namespace FSpot.Widgets {
	public class OpenWithMenu: Gtk.Menu {
#if GTK_SHARP_2_12_6
		public delegate void OpenWithHandler (AppInfo app_info);
#else
		public delegate void OpenWithHandler (Gnome.Vfs.MimeApplication app_info);
#endif
		public event OpenWithHandler ApplicationActivated;

		public delegate string [] TypeFetcher ();
		TypeFetcher type_fetcher;

		List<string> ignore_apps;
		public string [] IgnoreApp {
			get {
				if (ignore_apps == null)
					return null;
				return ignore_apps.ToArray ();
			}
		}

		bool show_icons = true;
		public bool ShowIcons {
			get { return show_icons; }
			set { show_icons = value; }
		}

#if !GTK_SHARP_2_12_6
		static OpenWithMenu ()
		{
			Gnome.Vfs.Vfs.Initialize ();
		}
#endif

		public OpenWithMenu (TypeFetcher type_fetcher) : this (type_fetcher, null)
		{
		}

		public OpenWithMenu (TypeFetcher type_fetcher, params string [] ignore_apps)
		{
			this.type_fetcher = type_fetcher;
			this.ignore_apps = new List<string> (ignore_apps);
		}

		//FIXME: this should be private and done on Draw()
		public void Populate (object sender, EventArgs args)
		{
			Widget [] dead_pool = Children;
			for (int i = 0; i < dead_pool.Length; i++)
				dead_pool [i].Destroy ();

#if GTK_SHARP_2_12_6
			foreach (AppInfo app in ApplicationsFor (type_fetcher ())) {
#else
			foreach (Gnome.Vfs.MimeApplication app in ApplicationsFor (type_fetcher ())) {
#endif
				AppMenuItem i = new AppMenuItem (app, show_icons);
				i.Activated += HandleItemActivated;
				Append (i);
			}

			if (Children.Length == 0) {
				MenuItem none = new Gtk.MenuItem (Catalog.GetString ("No applications available"));
				none.Sensitive = false;
				Append (none);
			}

			ShowAll ();
		}

#if GTK_SHARP_2_12_6
		AppInfo[] ApplicationsFor (string [] types)
#else
		Gnome.Vfs.MimeApplication[] ApplicationsFor (string [] types)
#endif
		{
#if GTK_SHARP_2_12_6
			List<AppInfo> app_infos = new List<AppInfo> ();
#else
			List<Gnome.Vfs.MimeApplication> app_infos = new List<Gnome.Vfs.MimeApplication> ();
#endif
			List<string> existing_ids = new List<string> ();
			foreach (string type in types)
#if GTK_SHARP_2_12_6
				foreach (AppInfo appinfo in AppInfoAdapter.GetAllForType (type)) {
					if (existing_ids.Contains (appinfo.Id))
						continue;
					if (!appinfo.SupportsUris ())
						continue;
					if (ignore_apps != null && ignore_apps.Contains (appinfo.Executable))
						continue;
					app_infos.Add (appinfo);
					existing_ids.Add (appinfo.Id);
				}
#else
				foreach (Gnome.Vfs.MimeApplication appinfo in Gnome.Vfs.Mime.GetAllApplications (type)) {
					if (existing_ids.Contains (appinfo.DesktopId))
						continue;
					if (!appinfo.SupportsUris ())
						continue;
					if (ignore_apps != null && ignore_apps.Contains (appinfo.BinaryName))
						continue;
					app_infos.Add (appinfo);
					existing_ids.Add (appinfo.DesktopId);
				}
#endif
			return app_infos.ToArray ();
		}

		private void HandleItemActivated (object sender, EventArgs args)
		{
			AppMenuItem app = (sender as AppMenuItem);

			if (ApplicationActivated != null)
				ApplicationActivated (app.App);
		}

		private class AppMenuItem : ImageMenuItem {

#if GTK_SHARP_2_12_6
			AppInfo app;
#else
			Gnome.Vfs.MimeApplication app;
#endif
#if GTK_SHARP_2_12_6
			public AppInfo App {
#else
			public Gnome.Vfs.MimeApplication App {
#endif
				get { return app; }
			}

#if GTK_SHARP_2_12_6
			public AppMenuItem (AppInfo app, bool show_icon) : base (app.Name)
#else
			public AppMenuItem (Gnome.Vfs.MimeApplication app, bool show_icon) : base (app.Name)
#endif
			{
				this.app = app;

				if (!show_icon)
					return;

//FIXME: GTK_SHARP_2_14 should provide a way to get the image directly out of app.Icon

				Pixbuf pixbuf = null;
#if GTK_SHARP_2_12_6
				if (app.Icon is ThemedIcon) {
					try {
						pixbuf = IconTheme.Default.ChooseIcon ((app.Icon as ThemedIcon).Names, 16, (IconLookupFlags)0).LoadIcon ();
					} catch (System.Exception) {
					}
				} else
					FSpot.Utils.Log.DebugFormat ("Loading icons from {0} is not implemented", app.Icon);
#else
				try {
					if (app.Icon.StartsWith ("/"))
						pixbuf = new Gdk.Pixbuf (app.Icon, 16, 16);
					else
						pixbuf = IconTheme.Default.LoadIcon (app.Icon, 16, (IconLookupFlags)0);
				} catch (System.Exception) {
					pixbuf = null;
				}
#endif

				if (pixbuf != null)
					Image = new Gtk.Image (pixbuf);
			}
		}
	}
}

