
using Gtk;

using System;
using System.Collections.Generic;

using FSpot;
using FSpot.Core;


public class PhotoVersionMenu : Menu {

	public IPhotoVersion Version {
		get; private set;
	}

	public delegate void VersionChangedHandler (PhotoVersionMenu menu);
	public event VersionChangedHandler VersionChanged;

	private Dictionary <MenuItem, IPhotoVersion> version_mapping;

	private void HandleMenuItemActivated (object sender, EventArgs args)
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
			Gtk.Label child = ((Gtk.Label) menu_item.Child);

			if (version == photo.DefaultVersion) {
				child.UseMarkup = true;
				child.Markup = String.Format ("<b>{0}</b>", version.Name);
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
