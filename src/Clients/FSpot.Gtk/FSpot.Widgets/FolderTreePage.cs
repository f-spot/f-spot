//
// FolderTreePage.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2009-2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Extensions;
using FSpot.Resources.Lang;

using Gtk;

namespace FSpot.Widgets
{
	public class FolderTreePage : SidebarPage
	{
		readonly FolderTreeView folder_tree_widget;

		public FolderTreePage ()
			: base (new ScrolledWindow (), Strings.Folders, "gtk-directory")
		{
			var scrolled_window = SidebarWidget as ScrolledWindow;
			folder_tree_widget = new FolderTreeView ();
			scrolled_window.Add (folder_tree_widget);
		}
	}
}
