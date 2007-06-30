using Gtk;
using GtkSharp;
using System;
using System.Text;
using System.Collections;

using Mono.Unix;

public class TagCommands {

	public enum TagType {
		Tag,
		Category
	}

	public class Create : FSpot.GladeDialog {
		TagStore tag_store;


		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private Entry tag_name_entry;
		[Glade.Widget] private Label prompt_label;
		[Glade.Widget] private Label already_in_use_label;

		[Glade.Widget]
		private OptionMenu category_option_menu;

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
				ok_button.Sensitive = false;
				already_in_use_label.Markup = String.Empty;
			} else if (TagNameExistsInCategory (tag_name_entry.Text, tag_store.RootCategory)) {
				ok_button.Sensitive = false;
				already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
			} else {
				ok_button.Sensitive = true;
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

	public class Edit : FSpot.GladeDialog {
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
			if (command.Execute (tag))
				icon_image.Pixbuf = tag.Icon;
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
				StringBuilder label_builder = new StringBuilder ();
				
				if (t.Category == category)
					history = i;
				
				i++;
				
				for (Category parent = category.Category; 
				     parent != null; 
				     parent = parent.Category)
					label_builder.Append ("  ");
				
				label_builder.Append (category.Name.Replace ("_", "__"));
				
				// FIXME escape underscores.
				MenuItem item = new MenuItem (label_builder.ToString ());
				menu.Append (item);
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
					t.Icon = icon_image.Pixbuf;

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

	public class EditIcon : FSpot.GladeDialog {
		Db db;
		Gtk.Window parent_window;
		FSpot.PhotoQuery query;
		FSpot.PhotoImageView image_view;
		IconView icon_view;

		[Glade.Widget] Gtk.Image preview_image;
		[Glade.Widget] ScrolledWindow photo_scrolled_window;
		[Glade.Widget] ScrolledWindow icon_scrolled_window;
		[Glade.Widget] Label photo_label;
		[Glade.Widget] SpinButton photo_spin_button;

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

		private void HandleSelectionChanged ()
		{
			int x, y, width, height;
			Gdk.Pixbuf tmp = null;
		       
			image_view.GetSelection (out x, out y, out width, out height);
			if (width > 0 && height > 0) 
				icon_view.Selection.Clear ();
				
			if (image_view.Pixbuf != null) {
				if (width > 0 && height > 0) {
					tmp = new Gdk.Pixbuf (image_view.Pixbuf, x, y, width, height);
					
					preview_image.Pixbuf = PixbufUtils.TagIconFromPixbuf (tmp);
					
					tmp.Dispose ();
				} else {
					preview_image.Pixbuf = PixbufUtils.TagIconFromPixbuf (image_view.Pixbuf);
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

		public void HandleIconViewSelectionChanged (FSpot.IBrowsableCollection collection) 
		{
			// FIXME this handler seems to be called twice for each selection change
			if (icon_view.Selection.Count > 0)
			{
				FSpot.IBrowsableItem item = icon_view.Selection [0];
				string path = item.DefaultVersionUri.LocalPath;
				try {
					preview_image.Pixbuf = new Gdk.Pixbuf (path);
					image_view.UnsetSelection ();
				} catch {
					// FIXME add a real exception handler here.
					System.Console.WriteLine ("Unable To Load image");
				}
			}
		}

		public bool Execute (Tag t)
		{
			this.CreateDialog ("edit_icon_dialog");

			this.Dialog.Title = String.Format (Catalog.GetString ("Edit Icon for Tag {0}"), t.Name);

			preview_image.Pixbuf = t.Icon;

			query = new FSpot.PhotoQuery (db.Photos);
			
			if (db.Tags.Hidden != null)
				query.Terms = FSpot.Query.OrTerm.FromTags (new Tag []{ t, db.Tags.Hidden });
			else 
				query.Terms = new FSpot.Query.Literal (t);

			image_view = new FSpot.PhotoImageView (query);
			image_view.SelectionXyRatio = 1.0;
			image_view.SelectionChanged += HandleSelectionChanged;
			image_view.PhotoChanged += HandlePhotoChanged;

			photo_scrolled_window.Add (image_view);

			if (query.Photos.Length > 0) {
				photo_spin_button.Wrap = true;
				photo_spin_button.Adjustment.Lower = 1.0;
				photo_spin_button.Adjustment.Upper = (double)query.Photos.Length;
				photo_spin_button.Adjustment.StepIncrement = 1.0;
				photo_spin_button.ValueChanged += HandleSpinButtonChanged;
				
				image_view.Item.Index = 0;
			} else {
				photo_spin_button.Sensitive = false;
				photo_spin_button.Value = 0.0;
			}			
			
			
			// FIXME this path choosing method is completely wrong/broken/evil it needs to be
			// based on real data but I want to get this release out.
			string theme_dir = System.IO.Path.Combine (FSpot.Defines.GNOME_ICON_THEME_PREFIX,
								   "share/icons/gnome/48x48/emblems");
			if (System.IO.Directory.Exists (theme_dir))
				icon_view = new IconView (new FSpot.DirectoryCollection (theme_dir));
			else if (System.IO.Directory.Exists ("/opt/gnome/share/icons/gnome/48x48/emblems"))
				icon_view = new IconView (new FSpot.DirectoryCollection ("/opt/gnome/share/icons/gnome/48x48/emblems"));
			else if (System.IO.Directory.Exists ("/usr/share/icons/gnome/48x48/emblems"))
				icon_view = new IconView (new FSpot.DirectoryCollection ("/usr/share/icons/gnome/48x48/emblems"));
			else // This will just load an empty collection if the directory doesn't exist.
				icon_view = new IconView (new FSpot.DirectoryCollection ("/usr/local/share/icons/gnome/48x48/emblems"));

			icon_scrolled_window.Add (icon_view);
			icon_view.ThumbnailWidth = 32;
			icon_view.DisplayTags = false;
			icon_view.DisplayDates = false;
			icon_view.Selection.Changed += HandleIconViewSelectionChanged;
			icon_view.Show();

			image_view.Show ();

			ResponseType response = (ResponseType) this.Dialog.Run ();
			bool success = false;

			if (response == ResponseType.Ok) {
				try {
					t.Icon = preview_image.Pixbuf;
					//db.Tags.Commit (t);
					success = true;
				} catch (Exception ex) {
					// FIXME error dialog.
					Console.WriteLine ("error {0}", ex);
				}
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
