using Gtk;
using System;

public class TagMenu : Menu {
	private TagStore tag_store;

	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;

	private class TagItem : Gtk.MenuItem {
		public Tag Value;

		public TagItem (Tag t) : base (t.Name) {
			Value = t;
		}
	}

	public TagMenu (TagStore store)
	{
		tag_store = store;
		Populate (store.RootCategory, this as Gtk.Menu);
	}
	
	public void Populate (Category cat, Gtk.Menu parent) {
		foreach (Tag t in cat.Children) {
			TagItem item = new TagItem (t);
			parent.Append (item);

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Length != 0) {
				Gtk.Menu submenu = new Menu ();
				TagItem subitem = new TagItem (t);
				subitem.Activated += HandleActivate;
				submenu.Append (subitem);
				submenu.Append (new Gtk.SeparatorMenuItem ());

				Populate (t as Category, submenu);
				item.Submenu = submenu;
			} else {
				item.Activated += HandleActivate;
			}
		} 
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
