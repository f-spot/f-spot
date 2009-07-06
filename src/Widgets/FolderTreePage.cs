/*
 * FSpot.Widgets.FolderTreePage.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;
using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreePage : SidebarPage
	{
		readonly FolderTreeView folder_tree_widget;
		
		public FolderTreePage () 
			: base (new ScrolledWindow (), Catalog.GetString ("Folders"), "gtk-directory")
		{
			ScrolledWindow scrolled_window = SidebarWidget as ScrolledWindow;
			folder_tree_widget = new FolderTreeView ();
			scrolled_window.Add (folder_tree_widget);
		}
	}
}
