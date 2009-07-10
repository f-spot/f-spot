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
using System;
using System.Text;
using System.Collections;

using Mono.Unix;
using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Widgets;

public class TagCommands {

	public enum TagType {
		Tag,
		Category
	}

	public class Create : GladeDialog {
		TagStore tag_store;


		[Glade.Widget] private Button create_button;
		[Glade.Widget] private Entry tag_name_entry;
		[Glade.Widget] private Label prompt_label;
		[Glade.Widget] private Label already_in_use_label;
		[Glade.Widget] private Label photo_label;
		[Glade.Widget] private ScrolledWindow photo_scrolled_window;
		[Glade.Widget] private OptionMenu category_option_menu;
		[Glade.Widget] private CheckButton auto_icon_checkbutton;

		Gtk.Widget parent_window;

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

		private void PopulateCategoryOptionMenu ()
		{
			categories = new ArrayList ();
			categories.Add (tag_store.RootCategory);
			PopulateCategories (categories, tag_store.RootCategory);

			Menu menu = new Menu ();

			foreach (Category category in categories)
				menu.Append (TagMenu.TagMenuItem.IndentedItem (category));

			category_option_menu.Sensitive = true;

			menu.ShowAll ();
			category_option_menu.Menu = menu;
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
					return categories [category_option_menu.History] as Category;
			}
			set {
				if ((value != null) && (categories.Count > 0)) {
					//System.Console.WriteLine("TagCreateCommand.set_Category(" + value.Name + ")");
					for (int i = 0; i < categories.Count; i++) {
						Category category = (Category)categories[i];
						// should there be an equals type method?
						if (value.Id == category.Id) {
							category_option_menu.SetHistory((uint)i);
							return;
						}
					}	
				}
			}
		}
		
		public Tag Execute (TagType type, Tag [] selection)
		{
			this.CreateDialog ("create_tag_dialog");

			Category default_category = null;
			if (selection.Length > 0) {
				if (selection [0] is Category)
					default_category = (Category) selection [0];
				else
					default_category = selection [0].Category;
			}

			this.Dialog.DefaultResponse = ResponseType.Ok;

			this.Dialog.Title = Catalog.GetString ("Create New Tag");
			prompt_label.Text = Catalog.GetString ("Name of New Tag:");
			this.auto_icon_checkbutton.Active = Preferences.Get<bool> (Preferences.TAG_ICON_AUTOMATIC);

			PopulateCategoryOptionMenu ();
			this.Category = default_category;
			Update ();
			tag_name_entry.GrabFocus ();

			ResponseType response = (ResponseType) this.Dialog.Run ();


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
					Console.WriteLine ("error {0}", ex);
				}
			}

			this.Dialog.Destroy ();
			return new_tag;
		}

		public Create (TagStore tag_store, Gtk.Window parent_window)
		{
			this.tag_store = tag_store;
			this.parent_window = parent_window;
		}
	}

}
