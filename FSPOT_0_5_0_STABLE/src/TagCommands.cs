/*
 * TagCommand.cs
 *
 * Author(s):
 * 	Larry Ewing <lewing@novell.com>
 * 	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using Gtk;
using GtkSharp;
using System;
using System.Text;
using System.Collections;

using Mono.Unix;
using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;

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

			PopulateCategoryOptionMenu ();
			this.Category = default_category;
			Update ();
			tag_name_entry.GrabFocus ();

			ResponseType response = (ResponseType) this.Dialog.Run ();

			Tag new_tag = null;
			if (response == ResponseType.Ok) {
				try {
					Category parent_category = Category;

					if (type == TagType.Category)
						new_tag = tag_store.CreateCategory (parent_category, tag_name_entry.Text) as Tag;
					else
						new_tag = tag_store.CreateTag (parent_category, tag_name_entry.Text);
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

	public class Edit : GladeDialog {
		Db db;
		Gtk.Window parent_window;
		Tag tag;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private Entry tag_name_entry;
		[Glade.Widget] private Label prompt_label;
		[Glade.Widget] private Label already_in_use_label;
		[Glade.Widget] private Gtk.Image icon_image;
		[Glade.Widget] private Button icon_button;
		[Glade.Widget] private OptionMenu category_option_menu;

		private ArrayList categories;

		private void HandleTagNameEntryChanged (object sender, EventArgs args)
		{
			Update ();
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

		string orig_name;
		string last_valid_name;
		private void Update ()
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

		private void PopulateCategories (ArrayList categories, Category parent)
		{
			foreach (Tag tag in parent.Children) {
				if (tag is Category && tag != this.tag && !this.tag.IsAncestorOf (tag)) {
					categories.Add (tag);
					PopulateCategories (categories, tag as Category);
				}
			}
		}

		private void HandleIconButtonClicked (object sender, EventArgs args)
		{
			TagCommands.EditIcon command = new TagCommands.EditIcon (db, parent_window);
			//FIXME
			if (command.Execute (tag)) {
				if (FSpot.ColorManagement.IsEnabled && tag.Icon != null) {
					icon_image.Pixbuf = tag.Icon.Copy();
					FSpot.ColorManagement.ApplyScreenProfile(icon_image.Pixbuf);
				}
				else
					icon_image.Pixbuf = tag.Icon;
			}
		}

		private void PopulateCategoryOptionMenu (Tag t)
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

		public bool Execute (Tag t) 
		{
			CreateDialog ("edit_tag_dialog");

			tag = t;
			this.Dialog.DefaultResponse = ResponseType.Ok;

			this.Dialog.Title = Catalog.GetString ("Edit Tag");
			prompt_label.Text = Catalog.GetString ("Tag Name:");

			orig_name = last_valid_name = t.Name;
			tag_name_entry.Text = t.Name;

			icon_image.Pixbuf = t.Icon;
			//FIXME
			if (FSpot.ColorManagement.IsEnabled && icon_image.Pixbuf != null) {
				icon_image.Pixbuf = icon_image.Pixbuf.Copy();
				FSpot.ColorManagement.ApplyScreenProfile (icon_image.Pixbuf);
			}
			PopulateCategoryOptionMenu  (t);
			
			icon_button.Clicked += HandleIconButtonClicked;
			icon_button.SetSizeRequest (48, 48);
			
			tag_name_entry.GrabFocus ();

			category_option_menu.Changed += HandleTagNameEntryChanged;
			ResponseType response = (ResponseType) this.Dialog.Run ();
			bool success = false;

			if (response == ResponseType.Ok) {
				try {
					t.Name = last_valid_name;
					t.Category = categories [category_option_menu.History] as Category;
					db.Tags.Commit (t, orig_name != t.Name);
					success = true;
				} catch (Exception ex) {
					// FIXME error dialog.
					Console.WriteLine ("error {0}", ex);
				}
			}
			
			this.Dialog.Destroy ();
			return success;
		}

		public Edit (Db db, Gtk.Window parent_window)
		{
			this.db = db;
			this.parent_window = parent_window;
		}
	}

	public class EditIcon : GladeDialog {
		Db db;
		Gtk.Window parent_window;
		FSpot.PhotoQuery query;
		FSpot.PhotoImageView image_view;
		Gtk.IconView icon_view;
		ListStore icon_store;
		string icon_name = null;
		Gtk.FileChooserButton external_photo_chooser;


		[Glade.Widget] Gtk.Image preview_image;
		[Glade.Widget] ScrolledWindow photo_scrolled_window;
		[Glade.Widget] ScrolledWindow icon_scrolled_window;
		[Glade.Widget] Label photo_label;
		[Glade.Widget] Label from_photo_label;
		[Glade.Widget] Label from_external_photo_label;
		[Glade.Widget] private Label predefined_icon_label;
		[Glade.Widget] SpinButton photo_spin_button;
		[Glade.Widget] HBox external_photo_chooser_hbox;
		[Glade.Widget] Button noicon_button;
		
		private Gdk.Pixbuf PreviewPixbuf_WithoutProfile;

		private Gdk.Pixbuf PreviewPixbuf {
			get { return preview_image.Pixbuf; }
			set {
				icon_name = null;
				preview_image.Pixbuf = value;
			}

		}

		private string IconName {
			get { return icon_name; }
			set {
				icon_name = value;	
				preview_image.Pixbuf = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, value, 48, (IconLookupFlags) 0);
			}
			
		}

		public FSpot.BrowsablePointer Item {
			get {
				return image_view.Item;
			}
		}
		
		private void HandleSpinButtonChanged (object sender, EventArgs args)
		{
			int value = photo_spin_button.ValueAsInt - 1;
			
			image_view.Item.Index = value;
		}

		private void HandleExternalFileSelectionChanged (object sender, EventArgs args)
		{	//Note: The filter on the FileChooserButton's dialog means that we will have a Pixbuf compatible uri here
			CreateTagIconFromExternalPhoto ();
		}

		private void CreateTagIconFromExternalPhoto ()
		{
			try {
				using (FSpot.ImageFile img = FSpot.ImageFile.Create(new Uri(external_photo_chooser.Uri))) {
					using (Gdk.Pixbuf external_image = img.Load ()) {
						PreviewPixbuf = PixbufUtils.TagIconFromPixbuf (external_image);
						PreviewPixbuf_WithoutProfile = PreviewPixbuf.Copy();
						FSpot.ColorManagement.ApplyScreenProfile (PreviewPixbuf);
					}
				}
			} catch (Exception) {
				string caption = Catalog.GetString ("Unable to load image");
				string message = String.Format (Catalog.GetString ("Unable to load \"{0}\" as icon for the tag"), 
									external_photo_chooser.Uri.ToString ());
				HigMessageDialog md = new HigMessageDialog (this.Dialog, 
									    DialogFlags.DestroyWithParent,
									    MessageType.Error,
									    ButtonsType.Close,
									    caption, 
									    message);
				md.Run();
				md.Destroy();
			}
		}

		private void HandleSelectionChanged ()
		{
			int x, y, width, height;
			Gdk.Pixbuf tmp = null;
		       
			image_view.GetSelection (out x, out y, out width, out height);
//			if (width > 0 && height > 0) 
//				icon_view.Selection.Clear ();
				
			if (image_view.Pixbuf != null) {
				if (width > 0 && height > 0) {
					tmp = new Gdk.Pixbuf (image_view.Pixbuf, x, y, width, height);
					
					//FIXME
					PreviewPixbuf = PixbufUtils.TagIconFromPixbuf (tmp);
					PreviewPixbuf_WithoutProfile = PreviewPixbuf.Copy();
					FSpot.ColorManagement.ApplyScreenProfile (PreviewPixbuf);
					
					tmp.Dispose ();
				} else {
					//FIXME
					PreviewPixbuf = PixbufUtils.TagIconFromPixbuf (image_view.Pixbuf);
					PreviewPixbuf_WithoutProfile = PreviewPixbuf.Copy();
					FSpot.ColorManagement.ApplyScreenProfile (PreviewPixbuf);
				}
			}
		}

		public void HandlePhotoChanged (FSpot.PhotoImageView sender)
		{
			int item = image_view.Item.Index;
			photo_label.Text = String.Format (Catalog.GetString ("Photo {0} of {1}"), 
							  item + 1, query.Count);

			photo_spin_button.Value = item + 1;
		}

		public void HandleIconSelectionChanged (object o, EventArgs args)
		{
			if (icon_view.SelectedItems.Length == 0)
				return;

			TreeIter iter;
			icon_store.GetIter (out iter, icon_view.SelectedItems [0]); 
			IconName = (string) icon_store.GetValue (iter, 0);
		}

		public bool FillIconView ()
		{
			icon_store.Clear ();
			string [] icon_list = FSpot.Global.IconTheme.ListIcons ("Emblems");
			foreach (string item_name in icon_list)
				icon_store.AppendValues (item_name, GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, item_name, 32, (IconLookupFlags) 0));
			return false;
		}


		public bool Execute (Tag t)
		{
			this.CreateDialog ("edit_icon_dialog");

			this.Dialog.Title = String.Format (Catalog.GetString ("Edit Icon for Tag {0}"), t.Name);

			PreviewPixbuf = t.Icon;

			query = new FSpot.PhotoQuery (db.Photos);
			
			if (db.Tags.Hidden != null)
				query.Terms = FSpot.OrTerm.FromTags (new Tag []{ t, db.Tags.Hidden });
			else 
				query.Terms = new FSpot.Literal (t);

			image_view = new FSpot.PhotoImageView (query);
			image_view.SelectionXyRatio = 1.0;
			image_view.SelectionChanged += HandleSelectionChanged;
			image_view.PhotoChanged += HandlePhotoChanged;

                        external_photo_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select Photo from file"),
                                                                 Gtk.FileChooserAction.Open);

			external_photo_chooser.Filter = new FileFilter();
			external_photo_chooser.Filter.AddPixbufFormats();
                        external_photo_chooser.LocalOnly = false;
                        external_photo_chooser_hbox.PackStart (external_photo_chooser);

    			Dialog.ShowAll ();
			external_photo_chooser.SelectionChanged += HandleExternalFileSelectionChanged;

			photo_scrolled_window.Add (image_view);

			if (query.Count > 0) {
				photo_spin_button.Wrap = true;
				photo_spin_button.Adjustment.Lower = 1.0;
				photo_spin_button.Adjustment.Upper = (double) query.Count;
				photo_spin_button.Adjustment.StepIncrement = 1.0;
				photo_spin_button.ValueChanged += HandleSpinButtonChanged;
				
				image_view.Item.Index = 0;
			} else {
				from_photo_label.Markup = String.Format (Catalog.GetString (
					"\n<b>From Photo</b>\n" +
					" You can use one of your library photos as an icon for this tag.\n" +
					" However, first you must have at least one photo associated\n" +
					" with this tag. Please tag a photo as '{0}' and return here\n" +
					" to use it as an icon."), t.Name); 
				photo_scrolled_window.Visible = false;
				photo_label.Visible = false;
				photo_spin_button.Visible = false;
			}			

			icon_store = new ListStore (typeof (string), typeof (Gdk.Pixbuf));

			icon_view = new Gtk.IconView (icon_store); 
			icon_view.PixbufColumn = 1;
			icon_view.SelectionMode = SelectionMode.Single;
			icon_view.SelectionChanged += HandleIconSelectionChanged;

			icon_scrolled_window.Add (icon_view);

			icon_view.Show();

			image_view.Show ();

			FSpot.Delay fill_delay = new FSpot.Delay (FillIconView);
			fill_delay.Start ();

			ResponseType response = (ResponseType) this.Dialog.Run ();
			bool success = false;

			if (response == ResponseType.Ok) {
				try {
					if (IconName != null) {
						t.ThemeIconName = IconName;
					} else {
						t.ThemeIconName = null;
						t.Icon = PreviewPixbuf_WithoutProfile;
					}
					//db.Tags.Commit (t);
					success = true;
				} catch (Exception ex) {
					// FIXME error dialog.
					Console.WriteLine ("error {0}", ex);
				}
			} else if (response == (ResponseType) (1)) {
				t.Icon = null;
				success = true;
			}
			
			this.Dialog.Destroy ();
			return success;
		}

		public EditIcon (Db db, Gtk.Window parent_window)
		{
			this.db = db;
			this.parent_window = parent_window;
		}
	}

}
