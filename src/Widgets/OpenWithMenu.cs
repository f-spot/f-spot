/*
 * FSpot.Widgets.OpenWithMenu.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 * 	Loz  <gnome2@flower.powernet.co.uk>
 * 	Gabriel Burt  <gabriel.burt@gmail.com>
 * 	Larry Ewing  <lewing@novell.com>
 *
 * Copyright (c) 2007 Stephane Delcroix
 * Copyright (c) 2007-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;
using GLib;
using GtkBeans;

using Mono.Unix;

namespace FSpot.Widgets {
	public class OpenWithMenu: Gtk.Menu {
		public event EventHandler<ApplicationActivatedEventArgs> ApplicationActivated;

		public delegate IEnumerable<string> TypeFetcher ();
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

			foreach (AppInfo app in ApplicationsFor (type_fetcher ())) {
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

		AppInfo[] ApplicationsFor (IEnumerable<string> types)
		{
			List<AppInfo> app_infos = new List<AppInfo> ();
			List<string> existing_ids = new List<string> ();
			foreach (string type in types)
				foreach (AppInfo appinfo in AppInfoAdapter.GetAllForType (type)) {
					if (existing_ids.Contains (appinfo.Id))
						continue;
					if (!appinfo.SupportsUris)
						continue;
					if (ignore_apps != null && ignore_apps.Contains (appinfo.Executable))
						continue;
					app_infos.Add (appinfo);
					existing_ids.Add (appinfo.Id);
				}
			return app_infos.ToArray ();
		}

		private void HandleItemActivated (object sender, EventArgs args)
		{
			AppMenuItem app = (sender as AppMenuItem);

			if (ApplicationActivated != null)
				ApplicationActivated (this, new ApplicationActivatedEventArgs (app.App));
		}

		private class AppMenuItem : ImageMenuItem
		{
			AppInfo app;
			public AppInfo App {
				get { return app; }
			}

			public AppMenuItem (AppInfo app, bool show_icon) : base (app.Name)
			{
				this.app = app;

				if (!show_icon)
					return;

				Image = GtkBeans.Image.NewFromIcon (app.Icon, IconSize.Menu);
#if GTK_2_16
				this.SetAlwaysShowImage (true);
#endif
			}
		}
	}
}

