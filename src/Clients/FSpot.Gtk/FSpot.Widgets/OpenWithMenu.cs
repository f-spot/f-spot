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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using GLib;

using Gtk;

using GtkBeans;

using Mono.Unix;

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
				var none = new Gtk.MenuItem (Catalog.GetString ("No applications available"));
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

