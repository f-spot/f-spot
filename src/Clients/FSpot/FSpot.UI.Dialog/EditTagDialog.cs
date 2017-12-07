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

using Mono.Unix;

using Gtk;

using FSpot.Core;
using FSpot.Database;
using FSpot.Settings;

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

			orig_name = last_valid_name = t.Name;
			tag_name_entry.Text = t.Name;

			icon_image.Pixbuf = t.Icon;
			Cms.Profile screen_profile;
			if (icon_image.Pixbuf != null && FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
				icon_image.Pixbuf = icon_image.Pixbuf.Copy ();
				FSpot.ColorManagement.ApplyProfile (icon_image.Pixbuf, screen_profile);
			}
			PopulateCategoryOptionMenu (t);

			tag_name_entry.GrabFocus ();

			category_option_menu.Changed += HandleTagNameEntryChanged;
		}

		string orig_name;
		string last_valid_name;

		public string TagName {
			get { return last_valid_name; }
		}

		public Category TagCategory {
			get { return categories [category_option_menu.Active] as Category;}
		}

		List<Tag> categories;

		void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			string name = tag_name_entry.Text;

			if (name == string.Empty) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = string.Empty;
			} else if (TagNameExistsInCategory (name, db.Tags.RootCategory)
				   && string.Compare (name, orig_name, true) != 0) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				ok_button.Sensitive = true;
				already_in_use_label.Markup = string.Empty;
				last_valid_name = tag_name_entry.Text;
			}
		}

		bool TagNameExistsInCategory (string name, Category category)
		{
			foreach (Tag tag in category.Children) {
				if (string.Compare (tag.Name, name, true) == 0)
					return true;

				if (tag is Category && TagNameExistsInCategory (name, tag as Category))
					return true;
			}

			return false;
		}

		void PopulateCategories (List<Tag> categories, Category parent)
		{
			foreach (Tag tag in parent.Children) {
				if (tag is Category && tag != this.tag && !this.tag.IsAncestorOf (tag)) {
					categories.Add (tag);
					PopulateCategories (categories, tag as Category);
				}
			}
		}

		void HandleIconButtonClicked (object sender, EventArgs args)
		{
			EditTagIconDialog dialog = new EditTagIconDialog (db, tag, this);

			ResponseType response = (ResponseType)dialog.Run ();
			if (response == ResponseType.Ok)
				if (dialog.ThemeIconName != null) {
					tag.ThemeIconName = dialog.ThemeIconName;
				} else {
					tag.ThemeIconName = null;
					tag.Icon = dialog.PreviewPixbuf;
				}
				else if (response == (ResponseType)1)
					tag.Icon = null;

			Cms.Profile screen_profile;
			if (tag.Icon != null && FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
				icon_image.Pixbuf = tag.Icon.Copy ();
				FSpot.ColorManagement.ApplyProfile (icon_image.Pixbuf, screen_profile);
			} else
				icon_image.Pixbuf = tag.Icon;

			dialog.Destroy ();
		}

		void PopulateCategoryOptionMenu (Tag t)
		{
			int history = 0;
			int i = 0;
			categories = new List<Tag> ();
			Category root = db.Tags.RootCategory;
			categories.Add (root);
			PopulateCategories (categories, root);

			category_option_menu.Clear ();

			CellRendererPixbuf cell2 = new CellRendererPixbuf ();
			category_option_menu.PackStart (cell2, false);
			category_option_menu.AddAttribute (cell2, "pixbuf", 0);

			CellRendererText cell = new CellRendererText ();
			category_option_menu.PackStart (cell, true);
			category_option_menu.AddAttribute (cell, "text", 1);

			ListStore store = new ListStore (new[] {typeof(Gdk.Pixbuf), typeof(string)});
			category_option_menu.Model = store;

			foreach (Category category in categories) {
				if (t.Category == category)
					history = i;

				i++;
				string categoryName = category.Name;
				Gdk.Pixbuf categoryImage = category.Icon;

				store.AppendValues (new object[] {
					categoryImage,
					categoryName
				});
			}

			category_option_menu.Sensitive = true;
			category_option_menu.Active = history;

			//category_option_menu.SetHistory ((uint)history);
			//category_option_menu.Active = (uint)history;
		}
	}
}
