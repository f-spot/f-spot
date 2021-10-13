//
// EditTagDialog.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using FSpot.Database;
using FSpot.Models;
using FSpot.Settings;

using Gtk;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class EditTagDialog : BuilderDialog
	{
		readonly Db db;
		Tag tag;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] Entry tag_name_entry;
		[GtkBeans.Builder.Object] Label already_in_use_label;
		[GtkBeans.Builder.Object] Gtk.Image icon_image;
		[GtkBeans.Builder.Object] Gtk.ComboBox category_option_menu;
#pragma warning restore 649

		public EditTagDialog (Db db, Tag t, Gtk.Window parent_window) : base ("EditTagDialog.ui", "edit_tag_dialog")
		{
			this.db = db;
			tag = t;
			TransientFor = parent_window;

			originalName = lastValidName = t.Name;
			tag_name_entry.Text = t.Name;

			/* FIXME, Tag icon support
			icon_image.Pixbuf = t.TagIcon.Icon;
			if (icon_image.Pixbuf != null && ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screen_profile)) {
				icon_image.Pixbuf = icon_image.Pixbuf.Copy ();
				ColorManagement.ApplyProfile (icon_image.Pixbuf, screen_profile);
			}
			*/
			PopulateCategoryOptionMenu (t);

			tag_name_entry.GrabFocus ();

			category_option_menu.Changed += HandleTagNameEntryChanged;
		}

		readonly string originalName;
		string lastValidName;

		public string TagName {
			get { return lastValidName; }
		}

		public Tag TagCategory {
			get { return categories[category_option_menu.Active]; }
		}

		List<Tag> categories;

		void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			string name = tag_name_entry.Text;

			if (string.IsNullOrEmpty (name)) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = string.Empty;
			} else if (TagNameExistsInCategory (name, db.Tags.RootCategory)
				   && string.Compare (name, originalName, StringComparison.OrdinalIgnoreCase) != 0) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				ok_button.Sensitive = true;
				already_in_use_label.Markup = string.Empty;
				lastValidName = tag_name_entry.Text;
			}
		}

		bool TagNameExistsInCategory (string name, Tag category)
		{
			foreach (Tag tag in category.Children) {
				if (string.Compare (tag.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
					return true;

				if (tag.IsCategory && TagNameExistsInCategory (name, tag))
					return true;
			}

			return false;
		}

		void PopulateCategories (List<Tag> categories, Tag parent)
		{
			foreach (Tag tag in parent.Children) {
				if (tag.IsCategory && tag != this.tag && !this.tag.IsAncestorOf (tag)) {
					categories.Add (tag);
					PopulateCategories (categories, tag);
				}
			}
		}

		void HandleIconButtonClicked (object sender, EventArgs args)
		{
			var dialog = new EditTagIconDialog (db, tag, this);

			ResponseType response = (ResponseType)dialog.Run ();
			if (response == ResponseType.Ok)
				if (dialog.ThemeIconName != null) {
					tag.ThemeIconName = dialog.ThemeIconName;
				} else {
					tag.ThemeIconName = null;
					tag.TagIcon.Icon = dialog.PreviewPixbuf;
				}
			else if (response == (ResponseType)1)
				tag.Icon = null;

			if (tag.Icon != null && ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screen_profile)) {
				icon_image.Pixbuf = tag.TagIcon.Icon.Copy ();
				ColorManagement.ApplyProfile (icon_image.Pixbuf, screen_profile);
			} else
				icon_image.Pixbuf = tag.TagIcon.Icon;

			dialog.Destroy ();
		}

		void PopulateCategoryOptionMenu (Tag t)
		{
			int history = 0;
			int i = 0;
			categories = new List<Tag> ();
			var root = db.Tags.RootCategory;
			categories.Add (root);
			PopulateCategories (categories, root);

			category_option_menu.Clear ();

			using CellRendererPixbuf cell2 = new CellRendererPixbuf ();
			category_option_menu.PackStart (cell2, false);
			category_option_menu.AddAttribute (cell2, "pixbuf", 0);

			using CellRendererText cell = new CellRendererText ();
			category_option_menu.PackStart (cell, true);
			category_option_menu.AddAttribute (cell, "text", 1);

			var store = new ListStore (typeof (Gdk.Pixbuf), typeof (string));
			category_option_menu.Model = store;

			foreach (var category in categories) {
				if (t.Category == category)
					history = i;

				i++;
				string categoryName = category.Name;
				Gdk.Pixbuf categoryImage = category.TagIcon.Icon;

				store.AppendValues (categoryImage, categoryName);
			}

			category_option_menu.Sensitive = true;
			category_option_menu.Active = history;

			//category_option_menu.SetHistory ((uint)history);
			//category_option_menu.Active = (uint)history;
		}
	}
}
