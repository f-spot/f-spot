/*
 * TagPopup.cs
 *
 * Author:
 *   Larry Ewing <lewing@novell.com>
 *
 * Copyright (c) 2004 Novell, Inc.
 *
 *
 */

using System;

public class TagPopup {
	public void Activate (Gdk.EventButton eb, Tag tag, Tag [] tags)
	{
		int count = MainWindow.Toplevel.SelectedIds ().Length;
		Gtk.Menu popup_menu = new Gtk.Menu ();
		
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Create New Tag"),
				      new EventHandler (MainWindow.Toplevel.HandleCreateNewCategoryCommand), true);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		if (tag == null)
			GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Edit Tag"), null, false);
		else {
			string editstr = String.Format (Mono.Posix.Catalog.GetString ("Edit Tag \"{0}\""), tag.Name.Replace ("_", "__"));
			GtkUtil.MakeMenuItem (popup_menu, editstr, delegate { MainWindow.Toplevel.HandleEditSelectedTagWithTag (tag); }, true);
		}

		GtkUtil.MakeMenuItem (popup_menu,
				      Mono.Posix.Catalog.GetPluralString ("Delete Tag", "Delete Tags", tags.Length),
				      new EventHandler (MainWindow.Toplevel.HandleDeleteSelectedTagCommand), tag != null && tags != null && tags.Length > 0);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
				      Mono.Posix.Catalog.GetPluralString ("Attach Tag To Selection", "Attach Tags To Selection", tags.Length),
				      new EventHandler (MainWindow.Toplevel.HandleAttachTagCommand), count > 0);

		GtkUtil.MakeMenuItem (popup_menu,
				      Mono.Posix.Catalog.GetPluralString ("Remove Tag From Selection", "Remove Tags From Selection", tags.Length),
				      new EventHandler (MainWindow.Toplevel.HandleRemoveTagCommand), count > 0);

		if (tags.Length > 1) {
			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Merge Tags"),
					      new EventHandler (MainWindow.Toplevel.HandleMergeTagsCommand), tags.Length > 1);

		}

		if (eb != null)
 			popup_menu.Popup (null, null, null, eb.Button, eb.Time);
		else
			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
	}
}
