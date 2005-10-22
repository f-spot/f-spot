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
	public void Activate (Gdk.EventButton eb, Tag tag)
	{
		int count = MainWindow.Toplevel.SelectedIds ().Length;
		Gtk.Menu popup_menu = new Gtk.Menu ();
		
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Create New Tag"),
				      new EventHandler (MainWindow.Toplevel.HandleCreateNewTagCommand), true);
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Create New Category"),
				      new EventHandler (MainWindow.Toplevel.HandleCreateNewCategoryCommand), true);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Edit Tag"),
				      new EventHandler (MainWindow.Toplevel.HandleEditSelectedTag), tag != null);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Delete Tag"),
				      new EventHandler (MainWindow.Toplevel.HandleDeleteSelectedTagCommand), tag != null);
				      
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Attach Tag To Selection"),
				      new EventHandler (MainWindow.Toplevel.HandleAttachTagCommand), count > 0);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Remove Tag From Selection"),
				      new EventHandler (MainWindow.Toplevel.HandleRemoveTagCommand), count > 0);

		popup_menu.Popup (null, null, null, eb.Button, eb.Time);
	}
}
