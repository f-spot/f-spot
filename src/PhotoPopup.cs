/*
 * PhotoPopup.cs
 *
 * Authors:
 *   Larry Ewing <lewing@novell.com>
 *   Vladimir Vukicevic <vladimir@pobox.com>
 *   Miguel de Icaza <miguel@ximian.com>
 *
 * Copyright (C) 2002 Vladimir Vukicevic
 * Copyright (C) 2003 Novell, Inc.
 *
 */

using System;
using Gtk;
using Gdk;

public class PhotoPopup {
	public void Activate (Gdk.EventButton eb) 
	{
		int count = MainWindow.Toplevel.SelectedIds ().Length;
		
		Gtk.Menu popup_menu = new Gtk.Menu ();
		bool have_selection = count > 0;
		bool have_multi = count > 1;

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Copy Image Location"), 
				      new EventHandler (MainWindow.Toplevel.HandleCopyLocation), have_selection);
		
		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Rotate Left"), "f-spot-rotate-270",
				      new EventHandler (MainWindow.Toplevel.HandleRotate270Command), have_selection);
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Rotate Right"), "f-spot-rotate-90",
				      new EventHandler (MainWindow.Toplevel.HandleRotate90Command), have_selection);

		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Remove From Catalog"), 
				      new EventHandler (MainWindow.Toplevel.HandleRemoveCommand), have_selection);
		GtkUtil.MakeMenuItem (popup_menu, Mono.Posix.Catalog.GetString ("Delete From Drive"),
				      new EventHandler (MainWindow.Toplevel.HandleDeleteCommand), have_selection);

		GtkUtil.MakeMenuSeparator (popup_menu);
		
		//
		// FIXME TagMenu is ugly.
		//
		MenuItem attach_item = new MenuItem (Mono.Posix.Catalog.GetString ("Attach Tag"));
		TagMenu attach_menu = new TagMenu (attach_item, MainWindow.Toplevel.Database.Tags);
		attach_menu.TagSelected += MainWindow.Toplevel.HandleAttachTagMenuSelected;
		attach_item.ShowAll ();
		popup_menu.Append (attach_item);

		//
		// FIXME finish the IPhotoSelection stuff and move the activate handler into the class
		// this current method is way too complicated.
		//
		MenuItem remove_item = new MenuItem (Mono.Posix.Catalog.GetString ("Remove Tag"));
		PhotoTagMenu remove_menu = new PhotoTagMenu ();
		remove_menu.TagSelected += MainWindow.Toplevel.HandleRemoveTagMenuSelected;
		remove_item.Submenu = remove_menu;
		remove_item.Activated += MainWindow.Toplevel.HandleTagMenuActivate;
		remove_item.ShowAll ();
		popup_menu.Append (remove_item);

		popup_menu.Popup (null, null, null, IntPtr.Zero, eb.Button, eb.Time);
	}   
}
