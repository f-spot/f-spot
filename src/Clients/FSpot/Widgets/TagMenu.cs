//
// TagMenu.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2006 Gabriel Burt
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

using Gtk;

using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;

using Hyena;

public class TagMenu : Menu
{
	TagStore tag_store;

	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;

	public EventHandler NewTagHandler { get; set; }

	public class TagMenuItem : ImageMenuItem
	{
		public Tag Value;

		public TagMenuItem (Tag t) : this (t, t.Name) { }

		public TagMenuItem (Tag t, string name) : base (name.Replace ("_", "__"))
		{
			Value = t;
			if (t.Icon != null) {
				Image = new Gtk.Image (t.SizedIcon);
				// FIXME:  Where did this method go?
				//this.SetAlwaysShowImage (true);
			}
		}

		public static TagMenuItem IndentedItem (Tag t)
		{
			var label_builder = new System.Text.StringBuilder ();

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
		}

		tag_store = store;
	}

	protected TagMenu (IntPtr raw) : base (raw) {}

	public int GetPosition (Tag t)
	{
		// FIXME right now this only works on flat menus

		int i = 0;
		foreach (Widget w in Children) {
			var item = w as TagMenuItem;
			if (item != null) {
				if (t == item.Value)
					return i;
			}
			i++;
		}

		return -1;
	}

	public void Populate (bool flat = false)
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
			var item = TagMenuItem.IndentedItem (t);
			parent.Append (item);
			item.ShowAll ();

			var subcat = t as Category;
			if (subcat != null && subcat.Children.Count != 0) {
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
			var item = new TagMenuItem (t);
			parent.Append (item);
			item.ShowAll ();

			Category subcat = t as Category;
			if (subcat != null && subcat.Children.Count != 0) {
				var submenu = new Menu ();
				Populate (t as Category, submenu);

				var sep = new Gtk.SeparatorMenuItem ();
				submenu.Prepend (sep);
				sep.ShowAll ();

				var subitem = new TagMenuItem (t);
				subitem.Activated += HandleActivate;
				submenu.Prepend (subitem);
				subitem.ShowAll ();

				item.Submenu = submenu;
			} else {
				item.Activated += HandleActivate;
			}
		}
	}

	void HandlePopulate (object obj, EventArgs args)
	{
		Populate ();
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
