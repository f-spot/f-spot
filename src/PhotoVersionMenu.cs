using Gtk;
using System;
using FSpot;
public class PhotoVersionMenu : Menu {
	private uint version_id;
	public uint VersionId {
		get {
			return version_id;
		}

		set {
			version_id = value;
		}
	}

	public delegate void VersionIdChangedHandler (PhotoVersionMenu menu);
	public event VersionIdChangedHandler VersionIdChanged;

	private struct MenuItemInfo {
		public MenuItem Item;
		public uint VersionId;

		public MenuItemInfo (MenuItem menu_item, uint id)
		{
			Item = menu_item;
			VersionId = id;
		}
	}

	private MenuItemInfo [] item_infos;

	// Lame way to emulate radio menu items since the the radio menu item API in GTK# is kinda busted.
	private void HandleMenuItemActivated (object sender, EventArgs args)
	{
		foreach (MenuItemInfo info in item_infos) {
			if (info.Item == sender && info.VersionId != VersionId) {
				VersionId = info.VersionId;
				if (VersionIdChanged != null)
					VersionIdChanged (this);
				break;
			}
		}
	}

	public PhotoVersionMenu (Photo photo)
	{
		version_id = photo.DefaultVersionId;

		uint [] version_ids = photo.VersionIds;
		item_infos = new MenuItemInfo [version_ids.Length];

		int i = 0;
		foreach (uint id in version_ids) {
			MenuItem menu_item = new MenuItem (photo.GetVersion (id).Name);
			menu_item.Show ();
			menu_item.Sensitive = true;
			Gtk.Label child = ((Gtk.Label)menu_item.Child);
			child.UseUnderline = false;
			if (photo.DefaultVersionId == id) {
				child.UseMarkup = true;
				child.Markup = "<b>" + photo.GetVersion (id).Name + "</b>";
			}
			menu_item.Activated += new EventHandler (HandleMenuItemActivated);

			item_infos [i ++] = new MenuItemInfo (menu_item, id);

			Append (menu_item);
		}

		if (version_ids.Length == 1) {
			MenuItem no_edits_menu_item = new MenuItem (Mono.Unix.Catalog.GetString ("(No Edits)"));
			no_edits_menu_item.Show ();
			no_edits_menu_item.Sensitive = false;
			Append (no_edits_menu_item);
		}
	}
}
