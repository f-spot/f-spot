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
		int photo_count = MainWindow.Toplevel.SelectedIds ().Length;
		int tags_count = tags.Length;

		Gtk.Menu popup_menu = new Gtk.Menu ();
		
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Create New Tag"),
				      new EventHandler (MainWindow.Toplevel.HandleCreateNewCategoryCommand), true);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
			Mono.Posix.Catalog.GetString ("Edit Tag"),
			delegate { MainWindow.Toplevel.HandleEditSelectedTagWithTag (tag); }, tag != null && tags_count == 1);

		GtkUtil.MakeMenuItem (popup_menu,
			Mono.Posix.Catalog.GetPluralString ("Delete Tag", "Delete Tags", tags_count),
			new EventHandler (MainWindow.Toplevel.HandleDeleteSelectedTagCommand), tag != null);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
				      Mono.Posix.Catalog.GetPluralString ("Attach Tag to Selection", "Attach Tags to Selection", tags_count),
				      new EventHandler (MainWindow.Toplevel.HandleAttachTagCommand), tag != null && photo_count > 0);

		GtkUtil.MakeMenuItem (popup_menu,
				      Mono.Posix.Catalog.GetPluralString ("Remove Tag From Selection", "Remove Tags From Selection", tags_count),
				      new EventHandler (MainWindow.Toplevel.HandleRemoveTagCommand), tag != null && photo_count > 0);

		if (tags_count > 1 && tag != null) {
			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Merge Tags"),
					      new EventHandler (MainWindow.Toplevel.HandleMergeTagsCommand), true);

		}

		if (eb != null)
 			popup_menu.Popup (null, null, null, eb.Button, eb.Time);
		else
			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
	}
}
