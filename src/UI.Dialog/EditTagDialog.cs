/*
 * FSpot.UI.Dialog.EditTagDialog.cs
 *
 * Author(s):
 * 	Larry Ewing <lewing@novell.com>
 * 	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2003-2009 Novell, Inc,
 * Copyright (c) 2007 Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.Collections;
using Mono.Unix;
using Gtk;

namespace FSpot.UI.Dialog
{
	public class EditTagDialog : BuilderDialog {
		Db db;
		Tag tag;

		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] Entry tag_name_entry;
		[GtkBeans.Builder.Object] Label prompt_label;
		[GtkBeans.Builder.Object] Label already_in_use_label;
		[GtkBeans.Builder.Object] Gtk.Image icon_image;
		[GtkBeans.Builder.Object] Button icon_button;
		[GtkBeans.Builder.Object] OptionMenu category_option_menu;


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
				icon_image.Pixbuf = icon_image.Pixbuf.Copy();
				FSpot.ColorManagement.ApplyProfile (icon_image.Pixbuf, screen_profile);
			}
			PopulateCategoryOptionMenu  (t);
			
			tag_name_entry.GrabFocus ();

			category_option_menu.Changed += HandleTagNameEntryChanged;
		}

		string orig_name;
		string last_valid_name;
		public string TagName {
			get { return last_valid_name; }
		}

		public Category TagCategory {
			get { return categories [category_option_menu.History] as Category;}
		}

		ArrayList categories;

		void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			string name = tag_name_entry.Text;

			if (name == String.Empty) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = String.Empty;
			} else if (TagNameExistsInCategory (name, db.Tags.RootCategory)
				   && String.Compare(name, orig_name, true) != 0) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				ok_button.Sensitive = true;
				already_in_use_label.Markup = String.Empty;
				last_valid_name = tag_name_entry.Text;
			}
		}

		bool TagNameExistsInCategory (string name, Category category)
		{
			foreach (Tag tag in category.Children) {
				if (String.Compare(tag.Name, name, true) == 0)
					return true;

				if (tag is Category && TagNameExistsInCategory (name, tag as Category))
					return true;
			}

			return false;
		}

		void PopulateCategories (ArrayList categories, Category parent)
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

			ResponseType response = (ResponseType) dialog.Run ();
			if (response == ResponseType.Ok) {
				if (dialog.ThemeIconName != null) {
					tag.ThemeIconName = dialog.ThemeIconName;
				} else {
					tag.ThemeIconName = null;
					tag.Icon = dialog.PreviewPixbuf;
				}
			} else if (response == (ResponseType)1)
				tag.Icon = null;

			Cms.Profile screen_profile;
			if (tag.Icon != null && FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
				icon_image.Pixbuf = tag.Icon.Copy();
				FSpot.ColorManagement.ApplyProfile(icon_image.Pixbuf, screen_profile);
			} else
				icon_image.Pixbuf = tag.Icon;
			
			dialog.Destroy ();
		}

		void PopulateCategoryOptionMenu (Tag t)
		{
			int history = 0;
			int i = 0;
			categories = new ArrayList ();
			Category root = db.Tags.RootCategory;
			categories.Add (root);
			PopulateCategories (categories, root);

			Menu menu = new Menu ();

			foreach (Category category in categories) {
				if (t.Category == category)
					history = i;
				
				i++;
				
				menu.Append (TagMenu.TagMenuItem.IndentedItem (category));
			}
			
			category_option_menu.Sensitive = true;

			menu.ShowAll ();
			category_option_menu.Menu = menu;
			category_option_menu.SetHistory ((uint)history);
		}
	}
}

