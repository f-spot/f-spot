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

using Gtk;

using Mono.Unix;

using FSpot.Extensions;

namespace FSpot.Widgets
{
	public class FolderTreePage : SidebarPage
	{
		readonly FolderTreeView folderTreeWidget;

		public FolderTreePage () : base (new ScrolledWindow (), Catalog.GetString ("Folders"), "gtk-directory")
		{
			var scrolledWindow = SidebarWidget as ScrolledWindow;
			folderTreeWidget = new FolderTreeView ();
			scrolledWindow.Add (folderTreeWidget);
		}
	}
}
