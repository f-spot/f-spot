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
using GLib;

using Mono.Unix;

namespace FSpot.Widgets {
	public class OpenWithMenu: Gtk.Menu {
		public delegate void OpenWithHandler (AppInfo app_info);
		public event OpenWithHandler ApplicationActivated;

		public delegate string [] TypeFetcher ();
		TypeFetcher type_fetcher;

		string [] types;
		bool populated = false;

		List<string> ignore_apps;
		public string [] IgnoreApp {
			get {
				if (ignore_apps == null)
					return null;
				return ignore_apps.ToArray ();
			}
		}

		bool show_icons = false;
		public bool ShowIcons {
			get { return show_icons; }
			set { show_icons = value; }
		}

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
			string [] types = type_fetcher ();

			if (this.types != types && populated) {
				populated = false;

				Widget [] dead_pool = Children;
				for (int i = 0; i < dead_pool.Length; i++)
					dead_pool [i].Destroy ();
			}

			if (populated)
				return;

			foreach (AppInfo app in ApplicationsFor (types)) {
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

			populated = true;
		}

		AppInfo[] ApplicationsFor (string [] types)
		{
			List<AppInfo> app_infos = new List<AppInfo> ();
			foreach (string type in types)
				foreach (AppInfo appinfo in AppInfoAdapter.GetAllForType (type)) {
					if (app_infos.Contains (appinfo))
						continue;
					if (!appinfo.SupportsUris ())
						continue;
					if (ignore_apps != null && ignore_apps.Contains (appinfo.Executable))
						continue;
					app_infos.Add (appinfo);
				}
			return app_infos.ToArray ();
		}

		private void HandleItemActivated (object sender, EventArgs args)
		{
			AppMenuItem app = (sender as AppMenuItem);

			if (ApplicationActivated != null)
				ApplicationActivated (app.App);
		}

		private class AppMenuItem : ImageMenuItem {

			AppInfo app;
			public AppInfo App {
				get { return app; }
			}

			public AppMenuItem (AppInfo app, bool show_icon) : base (app.Name)
			{
				this.app = app;

				if (!show_icon)
					return;

				Pixbuf pixbuf = null;
				if (app.Icon is ThemedIcon) {
					try {
						pixbuf = IconTheme.Default.ChooseIcon ((app.Icon as ThemedIcon).Names, 16, (IconLookupFlags)0).LoadIcon ();
					} catch (System.Exception) {
					}
				} else
					FSpot.Utils.Log.DebugFormat ("Loading icons from {0} is not implemented", app.Icon);

				if (pixbuf != null)
					Image = new Gtk.Image (pixbuf);
			}
		}
	}
}

