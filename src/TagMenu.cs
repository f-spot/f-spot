using Gtk;
using System;

public class TagMenu : Menu {
	private TagStore tag_store;

	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;

	public TagMenu (TagStore store)
	{
		tag_store = store;
		Populate (store.RootCategory, this as Gtk.Menu);
	}
	
	public void Populate (Category cat, Gtk.Menu parent) {
		foreach (Tag t in cat.Children) {
			Gtk.MenuItem item = new Gtk.MenuItem (t.Name);
			item.Activated += HandleActivate;
			parent.Append (item);

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Length != 0) {
				Gtk.Menu submenu = new Menu ();

				Populate (t as Category, submenu);
				item.Submenu = submenu;
			}
		}
	}
	
	void HandleActivate (object obj, EventArgs args)
	{
		if (TagSelected != null) {
			TagSelected (tag_store.RootCategory);
		}
	}

}
