//
// OpenWithMenu.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2006-2009 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2006 Gabriel Burt
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Resources.Lang;

using Gtk;

namespace FSpot.Widgets
{
	public class OpenWithMenu : Gtk.Menu
	{
		//public event EventHandler<ApplicationActivatedEventArgs> ApplicationActivated;

		public delegate IEnumerable<string> TypeFetcher ();
		readonly TypeFetcher type_fetcher;

		List<string> ignore_apps;
		public string[] IgnoreApp {
			get {
				if (ignore_apps == null)
					return null;
				return ignore_apps.ToArray ();
			}
		}

		public bool ShowIcons { get; set; } = true;

		public OpenWithMenu (TypeFetcher typeFetcher) : this (typeFetcher, null)
		{
		}

		public OpenWithMenu (TypeFetcher typeFetcher, params string[] ignoreApps)
		{
			type_fetcher = typeFetcher;
			ignore_apps = new List<string> (ignoreApps);
		}

		//FIXME: this should be private and done on Draw()
		public void Populate (object sender, EventArgs args)
		{
			Widget[] deadPool = Children;
			for (int i = 0; i < deadPool.Length; i++)
				deadPool[i].Destroy ();

			//foreach (var app in ApplicationsFor (type_fetcher ())) {
			//	AppMenuItem i = new AppMenuItem (app, show_icons);
			//	i.Activated += HandleItemActivated;
			//	Append (i);
			//}

			if (Children.Length == 0) {
				var none = new Gtk.MenuItem (Strings.NoApplicationsAvailable);
				none.Sensitive = false;
				Append (none);
			}

			ShowAll ();
		}

		//List<AppInfo> ApplicationsFor (IEnumerable<string> types)
		//{
		//	var app_infos = new List<AppInfo> ();
		//	var existing_ids = new List<string> ();
		//	foreach (string type in types)
		//		foreach (var appinfo in AppInfoAdapter.GetAllForType (type)) {
		//			if (existing_ids.Contains (appinfo.Id))
		//				continue;
		//			if (!appinfo.SupportsUris)
		//				continue;
		//			if (ignore_apps != null && ignore_apps.Contains (appinfo.Executable))
		//				continue;
		//			app_infos.Add (appinfo);
		//			existing_ids.Add (appinfo.Id);
		//		}
		//	return app_infos;
		//}

		void HandleItemActivated (object sender, EventArgs args)
		{
			var app = (sender as AppMenuItem);

			//ApplicationActivated?.Invoke (this, new ApplicationActivatedEventArgs (app.App));
		}

		class AppMenuItem : ImageMenuItem
		{
			//public AppInfo App { get; }

			//public AppMenuItem (AppInfo app, bool show_icon) : base (app.Name)
			//{
			//	App = app;

			//	if (!show_icon)
			//		return;

			//	//Image = GtkBeans.Image.NewFromIcon (app.Icon, IconSize.Menu);
			//	//this.SetAlwaysShowImage (true);
			//}
		}
	}
}

