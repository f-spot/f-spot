/*
 * IconViewPopup.cs
 *
 * Author(s):
 *   Vladimir Vukicevic <vladimir@pobox.com>
 *   Miguel de Icaza <miguel@ximian.com>
 *
 * Copyright (C) 2002 Vladimir Vukicevic
 * Copyright (C) 2003 Novell, Inc.
 *
 */

using System;
using System.Text;
using System.Collections;
using Gtk;
using Gdk;

public class IconViewPopup {
	IconView icon_view;
	int item_clicked;
	
	public IconViewPopup () {
	}

	public IconViewPopup (IconView il, int item_clicked)
	{
		this.item_clicked = item_clicked;
		IconView = il;
	}

	public IconView IconView {
		get {
			return icon_view;
		}
		set {
			icon_view = value;
		}
	}

	public void Activate (Gdk.EventButton eb)
	{
		Gtk.Menu popup_menu = new Gtk.Menu ();
		bool have_selection = true;
		bool have_multi = false;

		int count = icon_view.SelectedIdxCount;
		if (count == 0) {
			have_selection = false;
		} else if (count > 1) {
			have_multi = true;
		}

		GtkUtil.MakeMenuItem (popup_menu, "Copy Image Location", 
				      new EventHandler (Action_CopyImageLocation), have_selection);

		GtkUtil.MakeMenuSeparator (popup_menu);

		/*
		GtkUtil.MakeMenuItem (popup_menu, (have_multi ? "Cut Images" : "Cut Image"),
				      new EventHandler (Action_CutImage), false);
		GtkUtil.MakeMenuItem (popup_menu, (have_multi ? "Copy Images" : "Copy Image"),
				      new EventHandler (Action_CopyImage), have_selection);
		GtkUtil.MakeMenuItem (popup_menu, "Paste Images",
				      new EventHandler (Action_PasteImage), true);
		*/
		GtkUtil.MakeMenuItem (popup_menu, "Remove From Catalog", 
				      new EventHandler (MainWindow.Toplevel.HandleRemoveCommand), have_selection);
		GtkUtil.MakeMenuItem (popup_menu, "Delete From Drive",
				      new EventHandler (MainWindow.Toplevel.HandleDeleteCommand), have_selection);

		GtkUtil.MakeMenuSeparator (popup_menu);
		GtkUtil.MakeMenuItem (popup_menu, "Rotate Left",
				      new EventHandler (MainWindow.Toplevel.HandleRotate270Command), have_selection);
		GtkUtil.MakeMenuItem (popup_menu, "Rotate Right",
				      new EventHandler (MainWindow.Toplevel.HandleRotate90Command), have_selection);
		
		popup_menu.Popup (null, null, null, IntPtr.Zero, eb.Button, eb.Time);
	}

	void Action_CutImage (object o, EventArgs ea)
	{
	}

	void Action_CopyImage (object o, EventArgs ea)
	{
	}

	void Action_PasteImage (object o, EventArgs ea)
	{
	}

	void Action_CopyImageLocation (object o, EventArgs a)
	{
		Clipboard clipboard = Clipboard.Get (Atom.Intern ("PRIMARY", false));
		
		string name = System.IO.Path.GetFullPath (icon_view.GetFullFilename (item_clicked));
		clipboard.SetText (name);
		
	}
}
