using Gtk;
using GtkSharp;
using System;

public class PhotoVersionMenu : Menu {
	private uint version_id;
	public uint VersionId {
		get {
			return version_id;
		}

		set {
			version_id = value;
			UpdateMenuItems ();
		}
	}

	public delegate void VersionIdChangedHandler (PhotoVersionMenu menu);
	public event VersionIdChangedHandler VersionIdChanged;

	private struct MenuItemInfo {
		public CheckMenuItem MenuItem;
		public uint VersionId;

		public MenuItemInfo (CheckMenuItem menu_item, uint id)
		{
			MenuItem = menu_item;
			VersionId = id;
		}
	}

	private MenuItemInfo [] item_infos;
	static bool updating_menu_items;

	// Lame way to emulate radio menu items since the the radio menu item API in GTK# is kinda busted.
	private void HandleMenuItemActivated (object sender, EventArgs args)
	{
		if (updating_menu_items)
			return;

		foreach (MenuItemInfo info in item_infos) {
			if (info.MenuItem == sender) {
				VersionId = info.VersionId;
				if (VersionIdChanged != null)
					VersionIdChanged (this);
				break;
			}
		}
	}

	private void UpdateMenuItems ()
	{
		updating_menu_items = true;

		foreach (MenuItemInfo info in item_infos) {
			if (info.VersionId == version_id)
				info.MenuItem.Active = true;
			else
				info.MenuItem.Active = false;
		}

		updating_menu_items = false;
	}

	public PhotoVersionMenu (Photo photo)
	{
		version_id = photo.DefaultVersionId;

		uint [] version_ids = photo.VersionIds;
		item_infos = new MenuItemInfo [version_ids.Length];

		int i = 0;
		foreach (uint id in version_ids) {
			CheckMenuItem menu_item = new CheckMenuItem (photo.GetVersionName (id));
			menu_item.Show ();
			menu_item.Activated += new EventHandler (HandleMenuItemActivated);

			item_infos [i ++] = new MenuItemInfo (menu_item, id);

			Append (menu_item);
		}

		if (version_ids.Length == 1) {
			MenuItem no_edits_menu_item = new MenuItem (Mono.Posix.Catalog.GetString ("(No Edits)"));
			no_edits_menu_item.Show ();
			no_edits_menu_item.Sensitive = false;
			Append (no_edits_menu_item);
		}

		UpdateMenuItems ();
	}

	public static MenuItem NewCreateVersionMenuItem ()
	{
		MenuItem menu_item = new MenuItem ("Create New Version...");
		menu_item.Show ();
		return menu_item;
	}

	public static MenuItem NewDeleteVersionMenuItem ()
	{
		MenuItem menu_item = new MenuItem ("Delete Version");
		menu_item.Show ();
		return menu_item;
	}

	public static MenuItem NewRenameVersionMenuItem ()
	{
		MenuItem menu_item = new MenuItem ("Rename Version");
		menu_item.Show ();
		return menu_item;
	}
}
