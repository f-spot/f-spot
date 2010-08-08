/*
 * TagCommand.cs
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

using Gtk;
using Gdk;
using System;
using System.Text;
using System.Collections;

using Mono.Unix;
using FSpot;
using FSpot.Core;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Widgets;
using Hyena;

public class TagCommands {

	public enum TagType {
		Tag,
		Category
	}

	public class Create : BuilderDialog {
		TagStore tag_store;


		[GtkBeans.Builder.Object] private Button create_button;
		[GtkBeans.Builder.Object] private Entry tag_name_entry;
		[GtkBeans.Builder.Object] private Label prompt_label;
		[GtkBeans.Builder.Object] private Label already_in_use_label;
		[GtkBeans.Builder.Object] private ComboBox category_option_menu;
		[GtkBeans.Builder.Object] private CheckButton auto_icon_checkbutton;

		private ArrayList categories;

		private void PopulateCategories (ArrayList categories, Category parent)
		{
			foreach (Tag tag in parent.Children) {
				if (tag is Category) {
					categories.Add (tag);
					PopulateCategories (categories, tag as Category);
				}
			}
		}

		private string Indentation (Category category)
		{
			int indentations = 0;
			for (Category parent = category.Category;
			     parent != null && parent.Category != null;
			     parent = parent.Category)
				indentations++;
			return new string (' ', indentations*2);
		}

		private void PopulateCategoryOptionMenu ()
		{
			categories = new ArrayList ();
			categories.Add (tag_store.RootCategory);
			PopulateCategories (categories, tag_store.RootCategory);

			ListStore category_store = new ListStore (typeof(Pixbuf), typeof(string));

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

		private bool TagNameExistsInCategory (string name, Category category)
		{
			foreach (Tag tag in category.Children) {
				if (String.Compare(tag.Name, name, true) == 0)
					return true;

				if (tag is Category && TagNameExistsInCategory (name, tag as Category))
					return true;
			}

			return false;
		}

		private void Update ()
		{
			if (tag_name_entry.Text == String.Empty) {
				create_button.Sensitive = false;
				already_in_use_label.Markup = String.Empty;
			} else if (TagNameExistsInCategory (tag_name_entry.Text, tag_store.RootCategory)) {
				create_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				create_button.Sensitive = true;
				already_in_use_label.Markup = String.Empty;
			}
		}

		private void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			Update ();
		}

		private Category Category {
			get {
				if (categories.Count == 0)
					return tag_store.RootCategory;
				else
					return categories [category_option_menu.Active] as Category;
			}
			set {
				if ((value != null) && (categories.Count > 0)) {
					//System.Console.WriteLine("TagCreateCommand.set_Category(" + value.Name + ")");
					for (int i = 0; i < categories.Count; i++) {
						Category category = (Category)categories[i];
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

			this.DefaultResponse = ResponseType.Ok;

			this.Title = Catalog.GetString ("Create New Tag");
			prompt_label.Text = Catalog.GetString ("Name of New Tag:");
			this.auto_icon_checkbutton.Active = Preferences.Get<bool> (Preferences.TAG_ICON_AUTOMATIC);

			PopulateCategoryOptionMenu ();
			this.Category = default_category;
			Update ();
			tag_name_entry.GrabFocus ();

			ResponseType response = (ResponseType) this.Run ();


			Tag new_tag = null;
			if (response == ResponseType.Ok) {
				bool autoicon = this.auto_icon_checkbutton.Active;
				Preferences.Set (Preferences.TAG_ICON_AUTOMATIC, autoicon);
				try {
					Category parent_category = Category;

					if (type == TagType.Category)
						new_tag = tag_store.CreateCategory (parent_category, tag_name_entry.Text, autoicon) as Tag;
					else
						new_tag = tag_store.CreateTag (parent_category, tag_name_entry.Text, autoicon);
				} catch (Exception ex) {
					// FIXME error dialog.
					Log.Exception (ex);
				}
			}

			this.Destroy ();
			return new_tag;
		}

		public Create (TagStore tag_store) : base ("CreateTagDialog.ui", "create_tag_dialog")
		{
			this.tag_store = tag_store;
		}
	}
}
