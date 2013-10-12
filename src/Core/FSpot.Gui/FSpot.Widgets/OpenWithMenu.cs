//
// OpenWithMenu.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2013 Stephen Shaw
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

using Gtk;
using GLib;

using Mono.Unix;

namespace FSpot.Widgets
{
	public class OpenWithMenu: Menu
	{
		public event EventHandler<ApplicationActivatedEventArgs> ApplicationActivated;
		public delegate IEnumerable<string> TypeFetcher ();
		readonly TypeFetcher type_fetcher;
		public bool ShowIcons { get; set; }

		readonly List<string> ignore_apps;
		public string [] IgnoreApp {
			get {
				return (ignore_apps == null) ? null : ignore_apps.ToArray ();
			}
		}

		public OpenWithMenu (TypeFetcher typeFetcher) : this (typeFetcher, null)
		{
		}

		public OpenWithMenu (TypeFetcher typeFetcher, params string [] ignoreApps)
		{
			type_fetcher = typeFetcher;
			ignore_apps = new List<string> (ignoreApps);
			ShowIcons = true;
		}

		//FIXME: this should be private and done on Draw()
		public void Populate (object sender, EventArgs args)
		{
			Widget [] dead_pool = Children;
			for (int i = 0; i < dead_pool.Length; i++)
				dead_pool [i].Destroy ();

			foreach (IAppInfo app in ApplicationsFor (type_fetcher ())) {
				var i = new AppMenuItem (app, ShowIcons);
				i.Activated += HandleItemActivated;
				Append (i);
			}

			if (Children.Length == 0) {
				var none = new MenuItem (Catalog.GetString ("No applications available"));
				none.Sensitive = false;
				Append (none);
			}

			ShowAll ();
		}

		IAppInfo[] ApplicationsFor (IEnumerable<string> types)
		{
			var app_infos = new List<IAppInfo> ();
			var existing_ids = new List<string> ();
			foreach (string type in types)
				foreach (IAppInfo appinfo in AppInfoAdapter.GetAllForType (type)) {
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

		void HandleItemActivated (object sender, EventArgs args)
		{
			AppMenuItem app = (sender as AppMenuItem);

			if (ApplicationActivated != null)
				ApplicationActivated (this, new ApplicationActivatedEventArgs (app.App));
		}

		class AppMenuItem : ImageMenuItem
		{
			public IAppInfo App { get; private set; }

			public AppMenuItem (IAppInfo app, bool showIcon) : base (app.Name)
			{
				App = app;

				if (!showIcon)
					return;

				Image = Gtk.Image.NewFromIconName (app.Icon, IconSize.Menu);
				//this.SetAlwaysShowImage (true);
			}
		}
	}
}

