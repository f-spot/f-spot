using System;
using System.Collections;
using Gtk;

public class PhotoTagMenu : Menu {
	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;
	// This should be reworked to use a Selection interface to
	// extract the current selection
	private class TagItem : Gtk.MenuItem {
		public Tag Value;

		public TagItem (Tag t) : base (t.Name) {
			Value = t;
		}

		protected TagItem (IntPtr raw) : base (raw) {}
	}

	public PhotoTagMenu () : base () {
	}
	
	protected PhotoTagMenu (IntPtr raw) : base (raw) {}

	public void Populate (Photo [] photos) {
		Hashtable hash = new Hashtable ();
		foreach (Photo p in photos) {
			foreach (Tag t in p.Tags) {
				if (!hash.Contains (t.Id)) {
					hash.Add (t.Id, t);
				}
			}
		}
		
		foreach (Widget w in this.Children) {
			w.Destroy ();
		}
		
		if (hash.Count == 0) {
			/* Fixme this should really set parent menu
			   items insensitve */
			MenuItem item = new MenuItem ("(No Tags)");
			this.Append (item);
			item.Sensitive = false;
			item.ShowAll ();
		}

		foreach (Tag t in hash.Values) {
			TagItem item = new TagItem (t);
			this.Append (item);
			item.ShowAll ();
			item.Activated += HandleActivate;
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
