using Gtk;
using System;

public class TagMenu : Menu {
	private TagStore tag_store;
	private MenuItem parent_item;

	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;

	public class TagItem : Gtk.ImageMenuItem
	{
		public Tag Value;

		public TagItem (Tag t) : this (t, t.Name) { }
		
		public TagItem (Tag t, string name) : base (name.Replace ("_", "__"))
		{
			Value = t;
			if (t.Icon != null)
				this.Image = new Gtk.Image (t.Icon);
		}

		public static TagItem IndentedItem (Tag t)
		{
			System.Text.StringBuilder label_builder = new System.Text.StringBuilder ();
			
			for (Category parent = t.Category; 
			     parent != null && parent.Category != null;
			     parent = parent.Category)
				label_builder.Append ("  ");
			
			label_builder.Append (t.Name.Replace ("_", "__"));
			return new TagItem (t, label_builder.ToString ());
		}

		protected TagItem (IntPtr raw) : base (raw) {}
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

	public void Populate (bool flat)
	{ 
		if (flat)
			PopulateFlat (tag_store.RootCategory, this);
		else
			Populate (tag_store.RootCategory, this);
	}

        public void PopulateFlat (Category cat, Gtk.Menu parent)
	{
		foreach (Tag t in cat.Children) {
			TagItem item = TagItem.IndentedItem (t);
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
			TagItem item = new TagItem (t);
			parent.Append (item);
			item.ShowAll ();

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Length != 0) {
				Gtk.Menu submenu = new Menu ();
				Populate (t as Category, submenu);

				Gtk.SeparatorMenuItem sep = new Gtk.SeparatorMenuItem ();
				submenu.Prepend (sep);
				sep.ShowAll ();

				TagItem subitem = new TagItem (t);
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
			TagItem t = obj as TagItem;
			if (t != null)
				TagSelected (t.Value);
			else 
				Console.WriteLine ("Item was not a TagItem");
		}
	}
}
