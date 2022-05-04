//
// PhotoVersionMenu.cs
//
// Author:
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Resources.Lang;

using Gtk;

namespace FSpot
{
	public class PhotoVersionMenu : Menu
	{
		public IPhotoVersion Version { get; private set; }

		public delegate void VersionChangedHandler (PhotoVersionMenu menu);
		public event VersionChangedHandler VersionChanged;

		readonly Dictionary<MenuItem, IPhotoVersion> version_mapping;

		void HandleMenuItemActivated (object sender, EventArgs args)
		{
			var item = sender as MenuItem;

			if (item != null && version_mapping.ContainsKey (item)) {
				Version = version_mapping[item];
				VersionChanged (this);
			}
		}

		public PhotoVersionMenu (IPhoto photo)
		{
			Version = photo.DefaultVersion;

			version_mapping = new Dictionary<MenuItem, IPhotoVersion> ();

			foreach (var version in photo.Versions) {
				var menu_item = new MenuItem (version.Name);
				menu_item.Show ();
				menu_item.Sensitive = true;
				var child = (Label)menu_item.Child;

				if (version == photo.DefaultVersion) {
					child.UseMarkup = true;
					child.Markup = $"<b>{version.Name}</b>";
				}

				version_mapping.Add (menu_item, version);

				Append (menu_item);
			}

			if (version_mapping.Count == 1) {
				var no_edits_menu_item = new MenuItem (Strings.ParenNoEditsParen);
				no_edits_menu_item.Show ();
				no_edits_menu_item.Sensitive = false;
				Append (no_edits_menu_item);
			}
		}
	}
}