//
// IMenuGenerator.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2007 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Extensions
{
	public interface IMenuGenerator
	{
		Gtk.Menu GetMenu ();
		void OnActivated (object sender, System.EventArgs e);
	}
}
