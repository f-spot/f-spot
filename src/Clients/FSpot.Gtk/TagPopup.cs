//
// TagPopup.cs
//
// Author:
//   Paul Werner Bou <paul@purecodes.org>
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Nat Friedman <nat@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Paul Werner Bou
// Copyright (C) 2004, 2006 Larry Ewing
// Copyright (C) 2006 Gabriel Burt
// Copyright (C) 2005 Nat Friedman
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Unix;

using FSpot;
using FSpot.Core;
using FSpot.Query;
using FSpot.Utils;

public class TagPopup
{
	public void Activate (Gdk.EventButton eb, Tag tag, Tag [] tags)
	{
		int photo_count = App.Instance.Organizer.SelectedPhotos ().Count;
		int tags_count = tags.Length;

		Gtk.Menu popup_menu = new Gtk.Menu ();

		GtkUtil.MakeMenuItem (popup_menu,
                string.Format (Catalog.GetPluralString ("Find", "Find", tags.Length), tags.Length),
                "gtk-add",
                new EventHandler (App.Instance.Organizer.HandleIncludeTag),
                true
        );

		TermMenuItem.Create (tags, popup_menu);

		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Create New Tag..."), "tag-new",
				      App.Instance.Organizer.HandleCreateNewCategoryCommand, true);

		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetString ("Edit Tag..."), "gtk-edit",
			delegate {
			App.Instance.Organizer.HandleEditSelectedTagWithTag (tag); }, tag != null && tags_count == 1);

		GtkUtil.MakeMenuItem (popup_menu,
			Catalog.GetPluralString ("Delete Tag", "Delete Tags", tags_count), "gtk-delete",
			new EventHandler (App.Instance.Organizer.HandleDeleteSelectedTagCommand), tag != null);

		GtkUtil.MakeMenuSeparator (popup_menu);

		GtkUtil.MakeMenuItem (popup_menu,
				      Catalog.GetPluralString ("Attach Tag to Selection", "Attach Tags to Selection", tags_count), "gtk-add",
				      new EventHandler (App.Instance.Organizer.HandleAttachTagCommand), tag != null && photo_count > 0);

		GtkUtil.MakeMenuItem (popup_menu,
				      Catalog.GetPluralString ("Remove Tag From Selection", "Remove Tags From Selection", tags_count), "gtk-remove",
				      new EventHandler (App.Instance.Organizer.HandleRemoveTagCommand), tag != null && photo_count > 0);

		if (tags_count > 1 && tag != null) {
			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Merge Tags"),
					      new EventHandler (App.Instance.Organizer.HandleMergeTagsCommand), true);

		}

		if (eb != null) {
			popup_menu.Popup (null, null, null, eb.Button, eb.Time);
		} else {
			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		}
	}
}
