//
// CreateTagDialog.cs
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
using System.Linq;

using FSpot.Database;
using FSpot.Models;
using FSpot.Settings;

using Gdk;

using Gtk;

using Hyena;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class CreateTagDialog : BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Button create_button;
		[GtkBeans.Builder.Object] Entry tag_name_entry;
		[GtkBeans.Builder.Object] Label prompt_label;
		[GtkBeans.Builder.Object] Label already_in_use_label;
		[GtkBeans.Builder.Object] ComboBox category_option_menu;
		[GtkBeans.Builder.Object] CheckButton auto_icon_checkbutton;
#pragma warning restore 649

		readonly TagStore tag_store;
		List<Tag> categories;

		public CreateTagDialog (TagStore tag_store) : base ("CreateTagDialog.ui", "create_tag_dialog")
		{
			this.tag_store = tag_store;
		}

		void PopulateCategories (ICollection<Tag> categories, Tag parent)
		{
			foreach (Tag tag in parent.Children.Where (x => x.IsCategory)) {
				categories.Add (tag);
				PopulateCategories (categories, tag);
			}
		}

		string Indentation (Tag category)
		{
			int indentations = 0;
			for (var parent = category.Category; parent?.Category != null; parent = parent.Category)
				indentations++;

			return new string (' ', indentations * 2);
		}

		void PopulateCategoryOptionMenu ()
		{
			categories = new List<Tag> { tag_store.RootCategory };
			PopulateCategories (categories, tag_store.RootCategory);

			var categoryStore = new ListStore (typeof (Pixbuf), typeof (string));

			foreach (var category in categories) {
				categoryStore.AppendValues (category?.TagIcon?.SizedIcon, $"{Indentation (category)}{category.Name}");
			}

			category_option_menu.Sensitive = true;

			category_option_menu.Model = categoryStore;
			using var iconRenderer = new CellRendererPixbuf { Width = (int)Tag.TagIconSize };
			category_option_menu.PackStart (iconRenderer, true);

			using var textRenderer = new CellRendererText {
				Alignment = Pango.Alignment.Left, Width = 150
			};

			category_option_menu.PackStart (textRenderer, true);
			category_option_menu.AddAttribute (iconRenderer, "pixbuf", 0);
			category_option_menu.AddAttribute (textRenderer, "text", 1);
			category_option_menu.ShowAll ();
		}

		bool TagNameExistsInCategory (string name, Tag category)
		{
			foreach (Tag tag in category.Children) {
				if (string.Compare (tag.Name, name, true) == 0)
					return true;

				if (tag.IsCategory && TagNameExistsInCategory (name, tag))
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

		Tag Category {
			get {
				if (categories.Count == 0)
					return tag_store.RootCategory;
				return categories[category_option_menu.Active];
			}
			set {
				if ((value != null) && (categories.Count > 0)) {
					//System.Console.WriteLine("TagCreateCommand.set_Category(" + value.Name + ")");
					for (int i = 0; i < categories.Count; i++) {
						var category = categories[i];
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

		public Tag Execute (IEnumerable<Tag> selection)
		{
			Tag defaultCategory;
			if (selection.Any ()) {
				if (selection.First ().IsCategory)
					defaultCategory = selection.First ();
				else
					defaultCategory = selection.First ().Category;
			} else {
				defaultCategory = tag_store.RootCategory;
			}

			DefaultResponse = ResponseType.Ok;

			Title = Catalog.GetString ("Create New Tag");
			prompt_label.Text = Catalog.GetString ("Name of New Tag:");
			auto_icon_checkbutton.Active = Preferences.Get<bool> (Preferences.TagIconAutomatic);

			PopulateCategoryOptionMenu ();
			Category = defaultCategory;
			Update ();
			tag_name_entry.GrabFocus ();

			var response = (ResponseType)Run ();

			Tag newTag = null;
			if (response == ResponseType.Ok) {
				bool autoicon = auto_icon_checkbutton.Active;
				Preferences.Set (Preferences.TagIconAutomatic, autoicon);
				try {
					var parentCategory = Category;

					newTag = tag_store.CreateTag (parentCategory, tag_name_entry.Text, autoicon, true);
				} catch (Exception ex) {
					// FIXME error dialog.
					Log.Exception (ex);
				}
			}

			Destroy ();
			return newTag;
		}
	}
}
