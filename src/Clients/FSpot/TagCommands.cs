//
// TagCommands.cs
//
// Author:
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Peter Goetz <peter.gtz@gmail.com>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2010 Peter Goetz
// Copyright (C) 2004-2006 Larry Ewing
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

using Gtk;
using Gdk;

using Mono.Unix;

using FSpot.Core;
using FSpot.Database;
using FSpot.Settings;
using FSpot.UI.Dialog;

using Hyena;

public class TagCommands {

	public enum TagType {
		Tag,
		Category
	}

	public class Create : BuilderDialog
	{
		readonly TagStore tag_store;

		[GtkBeans.Builder.Object] Button create_button;
		[GtkBeans.Builder.Object] Entry tag_name_entry;
		[GtkBeans.Builder.Object] Label prompt_label;
		[GtkBeans.Builder.Object] Label already_in_use_label;
		[GtkBeans.Builder.Object] ComboBox category_option_menu;
		[GtkBeans.Builder.Object] CheckButton auto_icon_checkbutton;

		List<Tag> categories;

		void PopulateCategories (List<Tag> categories, Category parent)
		{
			foreach (Tag tag in parent.Children) {
				if (tag is Category) {
					categories.Add (tag);
					PopulateCategories (categories, tag as Category);
				}
			}
		}

		string Indentation (Category category)
		{
			int indentations = 0;
			for (Category parent = category.Category;
				 parent != null && parent.Category != null;
				 parent = parent.Category)
				indentations++;
			return new string (' ', indentations * 2);
		}

		void PopulateCategoryOptionMenu ()
		{
			categories = new List<Tag> ();
			categories.Add (tag_store.RootCategory);
			PopulateCategories (categories, tag_store.RootCategory);

			ListStore category_store = new ListStore (typeof (Pixbuf), typeof (string));

			foreach (Category category in categories) {
				category_store.AppendValues (category.SizedIcon, Indentation (category) + category.Name);
			}

			category_option_menu.Sensitive = true;

			category_option_menu.Model = category_store;
			var icon_renderer = new CellRendererPixbuf ();
			icon_renderer.Width = (int)Tag.TagIconSize;
			category_option_menu.PackStart (icon_renderer, true);

			var text_renderer = new CellRendererText ();
			text_renderer.Alignment = Pango.Alignment.Left;
			text_renderer.Width = 150;
			category_option_menu.PackStart (text_renderer, true);

			category_option_menu.AddAttribute (icon_renderer, "pixbuf", 0);
			category_option_menu.AddAttribute (text_renderer, "text", 1);
			category_option_menu.ShowAll ();
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

		void Update ()
		{
			if (tag_name_entry.Text == string.Empty) {
				create_button.Sensitive = false;
				already_in_use_label.Markup = string.Empty;
			} else if (TagNameExistsInCategory (tag_name_entry.Text, tag_store.RootCategory)) {
				create_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				create_button.Sensitive = true;
				already_in_use_label.Markup = string.Empty;
			}
		}

		void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			Update ();
		}

		Category Category {
			get {
				if (categories.Count == 0)
					return tag_store.RootCategory;
				return categories [category_option_menu.Active] as Category;
			}
			set {
				if ((value != null) && (categories.Count > 0)) {
					//System.Console.WriteLine("TagCreateCommand.set_Category(" + value.Name + ")");
					for (int i = 0; i < categories.Count; i++) {
						Category category = (Category)categories [i];
						// should there be an equals type method?
						if (value.Id == category.Id) {
							category_option_menu.Active = i;
							return;
						}
					}
				} else {
					category_option_menu.Active = 0;
				}
			}
		}

		public Tag Execute (TagType type, Tag [] selection)
		{
			Category default_category = null;
			if (selection.Length > 0) {
				if (selection [0] is Category)
					default_category = (Category) selection [0];
				else
					default_category = selection [0].Category;
			} else {
				default_category = tag_store.RootCategory;
			}

			DefaultResponse = ResponseType.Ok;

			Title = Catalog.GetString ("Create New Tag");
			prompt_label.Text = Catalog.GetString ("Name of New Tag:");
			auto_icon_checkbutton.Active = Preferences.Get<bool> (Preferences.TAG_ICON_AUTOMATIC);

			PopulateCategoryOptionMenu ();
			Category = default_category;
			Update ();
			tag_name_entry.GrabFocus ();

			ResponseType response = (ResponseType) Run ();


			Tag new_tag = null;
			if (response == ResponseType.Ok) {
				bool autoicon = this.auto_icon_checkbutton.Active;
				Preferences.Set (Preferences.TAG_ICON_AUTOMATIC, autoicon);
				try {
					Category parent_category = Category;

					if (type == TagType.Category)
						new_tag = tag_store.CreateCategory (parent_category, tag_name_entry.Text, autoicon);
					else
						new_tag = tag_store.CreateTag (parent_category, tag_name_entry.Text, autoicon);
				} catch (Exception ex) {
					// FIXME error dialog.
					Log.Exception (ex);
				}
			}

			Destroy ();
			return new_tag;
		}

		public Create (TagStore tag_store) : base ("CreateTagDialog.ui", "create_tag_dialog")
		{
			this.tag_store = tag_store;
		}
	}
}
