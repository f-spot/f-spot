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

using FSpot.Core;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot
{
	public class TagPopup
	{
		public void Activate (Gdk.EventButton eb, Tag tag, List<Tag> tags)
		{
			var photo_count = App.Instance.Organizer.SelectedPhotos ().Length;
			var tags_count = tags.Count;

			var popup_menu = new Gtk.Menu ();

			GtkUtil.MakeMenuItem (popup_menu,
					Strings.Find,
					"gtk-add",
					new EventHandler (App.Instance.Organizer.HandleIncludeTag),
					true
			);

			TermMenuItem.Create (tags, popup_menu);

			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Strings.CreateNewTag, "tag-new",
						  App.Instance.Organizer.HandleCreateNewCategoryCommand, true);

			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Strings.EditTag, "gtk-edit",
				delegate {
					App.Instance.Organizer.HandleEditSelectedTagWithTag (tag);
				}, tag != null && tags_count == 1);

			GtkUtil.MakeMenuItem (popup_menu, Strings.DeleteTag, "gtk-delete",
				new EventHandler (App.Instance.Organizer.HandleDeleteSelectedTagCommand), tag != null);

			GtkUtil.MakeMenuSeparator (popup_menu);

			GtkUtil.MakeMenuItem (popup_menu, Strings.AttachTagToSelection, "gtk-add",
						  new EventHandler (App.Instance.Organizer.HandleAttachTagCommand), tag != null && photo_count > 0);

			GtkUtil.MakeMenuItem (popup_menu, Strings.RemoveTagFromSelection, "gtk-remove",
						  new EventHandler (App.Instance.Organizer.HandleRemoveTagCommand), tag != null && photo_count > 0);

			if (tags_count > 1 && tag != null) {
				GtkUtil.MakeMenuSeparator (popup_menu);

				GtkUtil.MakeMenuItem (popup_menu, Strings.MergeTags,
							  new EventHandler (App.Instance.Organizer.HandleMergeTagsCommand), true);

			}

			if (eb != null) {
				popup_menu.Popup (null, null, null, eb.Button, eb.Time);
			} else {
				popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
			}
		}
	}
}