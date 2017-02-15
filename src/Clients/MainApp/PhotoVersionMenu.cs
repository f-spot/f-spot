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

using FSpot.Core;

public class PhotoVersionMenu : Menu
{
	public IPhotoVersion Version { get; private set; }

	public delegate void VersionChangedHandler (PhotoVersionMenu menu);
	public event VersionChangedHandler VersionChanged;

	readonly Dictionary<MenuItem, IPhotoVersion> version_mapping;

	void HandleMenuItemActivated (object sender, EventArgs args)
	{
		MenuItem item = sender as MenuItem;

		if (item != null && version_mapping.ContainsKey (item)) {
			Version = version_mapping [item];
			VersionChanged (this);
		}
	}

	public PhotoVersionMenu (IPhoto photo)
	{
		Version = photo.DefaultVersion;

		version_mapping = new Dictionary<MenuItem, IPhotoVersion> ();

		foreach (IPhotoVersion version in photo.Versions) {
			MenuItem menu_item = new MenuItem (version.Name);
			menu_item.Show ();
			menu_item.Sensitive = true;
			Gtk.Label child = ((Gtk.Label)menu_item.Child);

			if (version == photo.DefaultVersion) {
				child.UseMarkup = true;
				child.Markup = string.Format ("<b>{0}</b>", version.Name);
			}

			version_mapping.Add (menu_item, version);

			Append (menu_item);
		}

		if (version_mapping.Count == 1) {
			MenuItem no_edits_menu_item = new MenuItem (Mono.Unix.Catalog.GetString ("(No Edits)"));
			no_edits_menu_item.Show ();
			no_edits_menu_item.Sensitive = false;
			Append (no_edits_menu_item);
		}
	}
}
