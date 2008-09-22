/*
 * TagPopup.cs
 *
 * Author:
 *   Larry Ewing <lewing@novell.com>
 *
 * Copyright (c) 2004 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using Mono.Unix;
using FSpot;
using FSpot.Utils;

public class TagPopup {
	public void Activate (Gdk.EventButton eb, Tag tag, Tag [] tags)
	{
		int photo_count = MainWindow.Toplevel.SelectedPhotos ().Length;
		int tags_count = tags.Length;

		Gtk.Menu popup_menu = new Gtk.Menu ();

		GtkUtil.MakeMenuItem (popup_menu,
                String.Format (Catalog.GetPluralString ("Find", "Find", tags.Length), tags.Length),
                "gtk-add",
                new EventHandler (MainWindow.Toplevel.HandleIncludeTag),
                true
        );

        FSpot.TermMenuItem.Create (tags, popup_menu);

		GtkUtil.MakeMenuSeparator (popup_menu);
		
		GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Create New Tag..."), "tag-new",
				      MainWindow.Toplevel.HandleCreateNewCategoryCommand, true);

        GtkUtil.MakeMenuSeparator (popup_menu);
		
		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetString ("Edit Selected Tag..."), "gtk-edit",
			delegate { MainWindow.Toplevel.HandleEditSelectedTagWithTag (tag); }, tag != null && tags_count == 1);

		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetPluralString ("Delete Tag", "Delete Tags", tags_count), "gtk-delete",
			new EventHandler (MainWindow.Toplevel.HandleDeleteSelectedTagCommand), tag != null);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
				      Catalog.GetPluralString ("Attach Tag to Selection", "Attach Tags to Selection", tags_count), "gtk-add",
				      new EventHandler (MainWindow.Toplevel.HandleAttachTagCommand), tag != null && photo_count > 0);

		GtkUtil.MakeMenuItem (popup_menu,
				      Catalog.GetPluralString ("Remove Tag From Selection", "Remove Tags From Selection", tags_count), "gtk-remove",
				      new EventHandler (MainWindow.Toplevel.HandleRemoveTagCommand), tag != null && photo_count > 0);

		if (tags_count > 1 && tag != null) {
			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Merge Tags"),
					      new EventHandler (MainWindow.Toplevel.HandleMergeTagsCommand), true);

		}

		if (eb != null)
 			popup_menu.Popup (null, null, null, eb.Button, eb.Time);
		else
			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);

	}
}
