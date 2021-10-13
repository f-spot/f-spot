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
using System.Collections.Generic;
using System.Linq;

using FSpot.Models;
using FSpot.Query;
using FSpot.Utils;

using Mono.Unix;

namespace FSpot.Tagging
{
	public class TagPopup
	{
		public void Activate (Gdk.EventButton eb, Tag tag, IEnumerable<Tag> tags)
		{
			int photoCount = App.Instance.Organizer.SelectedPhotos ().Length;
			int tagsCount = tags.Count ();

			using var popupMenu = new Gtk.Menu ();

			GtkUtil.MakeMenuItem (popupMenu,
				string.Format (Catalog.GetPluralString ("Find", "Find", tags.Count ()), tags.Count ()),
				"gtk-add",
				App.Instance.Organizer.HandleIncludeTag,
				true
			);

			TermMenuItem.Create (tags, popupMenu);

			GtkUtil.MakeMenuSeparator (popupMenu);

			GtkUtil.MakeMenuItem (popupMenu, Catalog.GetString ("Create New Tag..."), "tag-new",
				App.Instance.Organizer.HandleCreateNewCategoryCommand, true);

			GtkUtil.MakeMenuSeparator (popupMenu);

			GtkUtil.MakeMenuItem (popupMenu,
				Catalog.GetString ("Edit Tag..."), "gtk-edit",
				delegate { App.Instance.Organizer.HandleEditSelectedTagWithTag (tag); },
				tag != null && tagsCount == 1);

			GtkUtil.MakeMenuItem (popupMenu,
				Catalog.GetPluralString ("Delete Tag", "Delete Tags", tagsCount), "gtk-delete",
				new EventHandler (App.Instance.Organizer.HandleDeleteSelectedTagCommand), tag != null);

			GtkUtil.MakeMenuSeparator (popupMenu);

			GtkUtil.MakeMenuItem (popupMenu,
				Catalog.GetPluralString ("Attach Tag to Selection", "Attach Tags to Selection", tagsCount), "gtk-add",
				new EventHandler (App.Instance.Organizer.HandleAttachTagCommand), tag != null && photoCount > 0);

			GtkUtil.MakeMenuItem (popupMenu,
				Catalog.GetPluralString ("Remove Tag From Selection", "Remove Tags From Selection", tagsCount),
				"gtk-remove",
				new EventHandler (App.Instance.Organizer.HandleRemoveTagCommand), tag != null && photoCount > 0);

			if (tagsCount > 1 && tag != null) {
				GtkUtil.MakeMenuSeparator (popupMenu);

				GtkUtil.MakeMenuItem (popupMenu, Catalog.GetString ("Merge Tags"),
					new EventHandler (App.Instance.Organizer.HandleMergeTagsCommand), true);

			}

			if (eb != null) {
				popupMenu.Popup (null, null, null, eb.Button, eb.Time);
			} else {
				popupMenu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
			}
		}
	}
}
