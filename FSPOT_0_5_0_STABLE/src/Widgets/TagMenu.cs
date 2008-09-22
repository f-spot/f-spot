using Gtk;
using System;
using FSpot;
using FSpot.Utils;

public class TagMenu : Menu {
	private TagStore tag_store;
	private MenuItem parent_item;

	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;
	
	private EventHandler new_tag_handler = null;
	public EventHandler NewTagHandler {
		get { return new_tag_handler; }
		set { new_tag_handler = value; }
	}

	public class TagMenuItem : Gtk.ImageMenuItem {
		public Tag Value;

		public TagMenuItem (Tag t) : this (t, t.Name) { }
		
		public TagMenuItem (Tag t, string name) : base (name.Replace ("_", "__"))
		{
			Value = t;
			if (t.Icon != null)
				this.Image = new Gtk.Image (t.SizedIcon);
		}

		public static TagMenuItem IndentedItem (Tag t)
		{
			System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
			
			for (Category parent = t.Category; 
			     parent != null && parent.Category != null;
			     parent = parent.Category)
				label_builder.Append ("  ");
			
			label_builder.Append (t.Name);
			return new TagMenuItem (t, label_builder.ToString ());
		}

		protected TagMenuItem (IntPtr raw) : base (raw) {}
	}

	public TagMenu (MenuItem item, TagStore store) 
	{
		if (item != null) {
			item.Submenu = this;
			item.Activated += HandlePopulate;
			parent_item = item;
		}

		tag_store = store;
	}

	protected TagMenu (IntPtr raw) : base (raw) {}

	public void Populate ()
	{
		Populate (false);
	}

	public int GetPosition (Tag t)
	{
		// FIXME right now this only works on flat menus

		int i = 0;
		foreach (Widget w in this.Children) {
			TagMenuItem item = w as TagMenuItem;
			if (item != null) {
				if (t == item.Value)
					return i;
			}
			i++;
		}
		
		return -1;
	}

	public void Populate (bool flat)
	{ 
		if (flat)
			PopulateFlat (tag_store.RootCategory, this);
		else
			Populate (tag_store.RootCategory, this);

		if (NewTagHandler != null) {
			GtkUtil.MakeMenuSeparator (this);
			GtkUtil.MakeMenuItem (this, Mono.Unix.Catalog.GetString ("Create New Tag..."),
					"tag-new", NewTagHandler, true);
		}
	}

        public void PopulateFlat (Category cat, Gtk.Menu parent)
	{
		foreach (Tag t in cat.Children) {
			TagMenuItem item = TagMenuItem.IndentedItem (t);
			parent.Append (item);
			item.ShowAll ();

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Length != 0) {
				PopulateFlat (t as Category, parent);
			} else {
				item.Activated += HandleActivate;
			}
		} 
	}

	public void Populate (Category cat, Gtk.Menu parent)
	{
		Widget [] dead_pool = parent.Children;
		for (int i = 0; i < dead_pool.Length; i++)
			dead_pool [i].Destroy ();

		foreach (Tag t in cat.Children) {
			TagMenuItem item = new TagMenuItem (t);
			parent.Append (item);
			item.ShowAll ();

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Length != 0) {
				Gtk.Menu submenu = new Menu ();
				Populate (t as Category, submenu);

				Gtk.SeparatorMenuItem sep = new Gtk.SeparatorMenuItem ();
				submenu.Prepend (sep);
				sep.ShowAll ();

				TagMenuItem subitem = new TagMenuItem (t);
				subitem.Activated += HandleActivate;
				submenu.Prepend (subitem);
				subitem.ShowAll ();

				item.Submenu = submenu;
			} else {
				item.Activated += HandleActivate;
			}
		} 
	}
	
	private void HandlePopulate (object obj, EventArgs args)
	{
		this.Populate ();
	}
	
	void HandleActivate (object obj, EventArgs args)
	{
		if (TagSelected != null) {
			TagMenuItem t = obj as TagMenuItem;
			if (t != null)
				TagSelected (t.Value);
			else 
				Log.Debug ("TagMenu.HandleActivate: Item was not a TagMenuItem");
		}
	}
}
