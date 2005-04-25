//
// TagSelectionWidget.cs
//
// Copyright (C) 2004 Novell, Inc.
//
//
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using GLib;
using Gdk;
using Gtk;
using GtkSharp;
using System.Collections;
using System.IO;
using System;

public class TagSelectionWidget : TreeView {
	TagSelectionWidget widget;
	private TagStore tag_store;

	// FIXME this is a hack.
	private static Pixbuf empty_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);


	// Selection management.

	// Hash of the IDs of the selected tags.
	private Hashtable selection;

	public Tag TagAtPosition (int x, int y) 
	{
		TreePath path;
		TreeIter iter;

		// Work out which tag we're dropping onto
		if (!this.GetPathAtPos (x, y, out path))
			return null;

		if (!Model.GetIter (out iter, path))
			return null;

		GLib.Value value = new GLib.Value ();

		Model.GetValue (iter, 0, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		return tag;
	}

	public void Select (Tag tag)
	{
		if (! selection.Contains (tag.Id))
			selection.Add (tag.Id, tag);
	}

	public void Unselect (Tag tag)
	{
		selection.Remove (tag.Id);
	}

	public void UnselectTagsForCategory (Category category)
	{
		foreach (Tag t in category.Children) {
			Unselect (t);
			if (t is Category)
				UnselectTagsForCategory (t as Category);
		}
	}

	public bool IsSelected (Tag tag)
	{
		if (tag == tag_store.RootCategory)
			return false;
		else if (selection.ContainsKey (tag.Id))
			return true;
		else if (tag.Category != tag_store.RootCategory && IsSelected (tag.Category))
			return true;
		else
			return false;
	}

	private void GetSelectionForCategory (ArrayList selection, Category category)
	{
		foreach (Tag t in category.Children) {
			if (IsSelected (t))
				selection.Add (t);
			if (t is Category)
				GetSelectionForCategory (selection, t as Category);
		}
	}

	public delegate void SelectionChangedHandler (object me);
	public event SelectionChangedHandler SelectionChanged;

	public Tag [] TagSelection {
		get {
			ArrayList selection = new ArrayList ();
			GetSelectionForCategory (selection, tag_store.RootCategory);
			return (Tag []) selection.ToArray (typeof (Tag));
		}

		set {
			UnselectTagsForCategory (tag_store.RootCategory);

			foreach (Tag t in value)
				Select (t);

			//FIXME this should really just toggle the items not rebuild the list
			Update ();
			SelectionChanged (this);
		}
	}


	// Loading up the store.

	private void LoadCategory (Category category, TreeIter parent_iter)
	{
		Tag [] tags = category.Children;

		foreach (Tag t in tags) {
			TreeIter iter = (Model as TreeStore).AppendValues (parent_iter, t.Id);
			if (t is Category)
				LoadCategory (t as Category, iter);
		}
	}

	public Tag [] TagHighlight () {
		TreeModel model;
		TreeIter iter;

		TreePath [] rows = Selection.GetSelectedRows(out model);

		Tag [] tags = new Tag [rows.Length];
		int i = 0;

		foreach (TreePath path in rows) {
			GLib.Value value = new GLib.Value ();
			Model.GetIter (out iter, path);
			Model.GetValue (iter, 0, ref value);
			uint tag_id = (uint) value;
			tags[i] = tag_store.Get (tag_id) as Tag;
			i++;
		}
		return tags;
	}

	public void Update ()
	{
		(Model as TreeStore).Clear ();

		// GRRR We have to special case the root because I can't pass null for a
		// Gtk.TreeIter (since it's a struct, and not a class).
		// FIXME: This should be fixed in GTK#...  It's gross.

		foreach (Tag t in tag_store.RootCategory.Children) {
			TreeIter iter = (Model as TreeStore).AppendValues (t.Id);
			if (t is Category)
				LoadCategory (t as Category, iter);
		}
	}


	// Event handlers.

	private void OnCellToggled (object renderer, ToggledArgs args)
	{
		TreePath path = new TreePath (args.Path);

		TreeIter iter;
		Model.GetIter (out iter, path);

		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, 0, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		// Tags under an unselected category are always conceptually unselected.
		// They appear as selected just in virtue of being children of a selected category.
		if (! IsSelected (tag.Category)) {
			if (IsSelected (tag))
				Unselect (tag);
			else
				Select (tag);

			(Model as TreeStore).EmitRowChanged (path, iter);

			// Make sure that if you unselect the category the tags are unselected as well.
			if (tag is Category)
				UnselectTagsForCategory (tag as Category);
		}

		if (SelectionChanged != null)
			SelectionChanged (this);
	}


	// Data functions.

	private void SetBackground (CellRenderer renderer, Tag tag)
	{
		// FIXME this should be themable but Gtk# doesn't give me access to the proper
		// members in GtkStyle for that.
		if (tag is Category)
			renderer.CellBackground = "#cccccc";
		else
			renderer.CellBackground = "#ffffff";
	}

	private void CheckBoxDataFunc (TreeViewColumn column,
				       CellRenderer renderer,
				       TreeModel model,
				       TreeIter iter)
	{
		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, 0, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		SetBackground (renderer, tag);
		(renderer as CellRendererToggle).Active = IsSelected (tag);
	}

	private void IconDataFunc (TreeViewColumn column, 
				   CellRenderer renderer,
				   TreeModel model,
				   TreeIter iter)
	{
		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, 0, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		SetBackground (renderer, tag);

		// FIXME I can't set the Pixbuf to null, not sure if it's a GTK# bug...
		if (tag.Icon != null)
			(renderer as CellRendererPixbuf).Pixbuf = tag.Icon;
		else
			(renderer as CellRendererPixbuf).Pixbuf = empty_pixbuf;
	}

	private void NameDataFunc (TreeViewColumn column,
				   CellRenderer renderer,
				   TreeModel model,
				   TreeIter iter)
	{
		// FIXME not sure why it happens...
		if (model == null)
			return;

		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, 0, ref value);
		uint tag_id = (uint) value;

		Tag tag = tag_store.Get (tag_id) as Tag;

		SetBackground (renderer, tag);

		(renderer as CellRendererText).Text = tag.Name;
	}

	private bool TreeIterForTag(Tag tag, out TreeIter iter) {
		TreeIter root = TreeIter.Zero;
		bool valid = Model.GetIterFirst(out root);
		while (valid) {
			if (TreeIterForTagRecurse(tag, root, out iter))
				return true;
			valid = Model.IterNext (ref root);
		}
		return false;
    }

	// Depth first traversal
	private bool TreeIterForTagRecurse(Tag tag, TreeIter parent, out TreeIter iter) {
		bool valid = Model.IterChildren(out iter, parent);
		while (valid) {
			if (TreeIterForTagRecurse(tag, iter, out iter))
				return true;
			valid = Model.IterNext (ref iter);
		}
		GLib.Value value = new GLib.Value();
		Model.GetValue(parent, 0, ref value);
		iter = parent;
		if (tag.Id == (uint) value)
			return true;
		return false;
	}
	
	// Copy a branch of the tree to a new parent
	// (note, this doesn't work generically as it only copies the first value of each node)
	private void CopyBranch(TreeIter src, TreeIter dest, bool is_root, bool is_parent) {
		TreeIter copy, iter;
		GLib.Value value = new GLib.Value();
		TreeStore store = Model as TreeStore;
		bool valid;
		
		store.GetValue(src, 0, ref value);
		if (is_parent) {
			Tag tag = (Tag)tag_store.Get((uint)value);
			// we need to figure out where to insert it in the correct order
			copy = InsertInOrder(dest, is_root, tag);
		} else { 
			copy = store.AppendValues(dest, (uint)value);
		}
		
		valid = Model.IterChildren(out iter, src);
		while (valid) {
			// child nodes are already ordered
			CopyBranch(iter, copy, false, false);
			valid = Model.IterNext (ref iter);
		}
	}

	// insert tag into the correct place in the tree, with parent. return the new TagIter in iter.
	private TreeIter InsertInOrder(TreeIter parent, bool is_root, Tag tag) {
		TreeIter iter;
		TreeStore store = Model as TreeStore;
		Tag compare;
		bool valid;
		if (is_root)
			valid = store.GetIterFirst(out iter);
		else
			valid = store.IterChildren(out iter, parent);
			
		while (valid) {
			//I have no desire to figure out a more performant sort over this...
			GLib.Value value = new GLib.Value();
			store.GetValue(iter, 0, ref value);
			compare = (Tag)tag_store.Get((uint)value);
			if (compare.CompareTo(tag) > 0) {
				store.InsertBefore(out iter, iter);
				store.SetValue(iter, 0, tag.Id);
				return iter;
			}
			valid = store.IterNext(ref iter);
		}
		if (is_root) 
			store.Append(out iter);
		else
			iter = store.Append(parent);
		store.SetValue(iter, 0, tag.Id);
		return iter;
	}	
			
	private void HandleTagDeleted(Tag tag) {
		TreeIter iter;
		if (TreeIterForTag(tag, out iter)) 
			(Model as TreeStore).Remove(ref iter);
	}
	
	private void HandleTagCreated(Tag tag) {
		TreeIter iter;
		if (TreeIterForTag(tag.Category, out iter)) {
			// create dialog doesn't let you create a top level tag...
			InsertInOrder(iter, false, tag);
		}
	}
	
	private void HandleTagChanged(Tag tag) {
		TreeStore store = Model as TreeStore;
		TreeIter iter, category_iter, parent_iter;
		TreeIterForTag(tag, out iter);
		bool category_valid = TreeIterForTag(tag.Category, out category_iter);
		bool parent_valid = Model.IterParent(out parent_iter, iter);
		if ((category_valid && (category_iter.Equals(parent_iter))) || (!category_valid && !parent_valid)) {
			// if we haven't been reparented
			TreePath path = store.GetPath(iter); 
			store.EmitRowChanged(path, iter);
		} else {
			// It is a bit tougher. We need to do an annoying clone of structs...
			CopyBranch(iter, category_iter, !category_valid, true);
			store.Remove(ref iter);
		}
	}
	// Constructor.

	public TagSelectionWidget (TagStore tag_store)
		: base (new TreeStore (typeof (uint)))
	{
		HeadersVisible = false;
		Selection.Mode = SelectionMode.Multiple;

		CellRendererToggle toggle_renderer = new CellRendererToggle ();
		toggle_renderer.Toggled += new ToggledHandler (OnCellToggled);

		AppendColumn ("check", toggle_renderer, new TreeCellDataFunc (CheckBoxDataFunc));
		AppendColumn ("icon", new CellRendererPixbuf (), new TreeCellDataFunc (IconDataFunc));
		AppendColumn ("name", new CellRendererText (), new TreeCellDataFunc (NameDataFunc));

		this.tag_store = tag_store;
		selection = new Hashtable ();

		Update ();
		ExpandAll ();
		tag_store.TagDeleted += new TagDeletedHandler(HandleTagDeleted);
		tag_store.TagCreated += new TagCreatedHandler(HandleTagCreated);
		tag_store.TagChanged += new TagChangedHandler(HandleTagChanged);
	}


#if TEST_TAG_SELECTION_WIDGET

	class Test {

		private TagSelectionWidget selection_widget;

		private void OnSelectionChanged ()
		{
			Console.WriteLine ("Selection changed:");

			foreach (Tag t in selection_widget.TagSelection)
				Console.WriteLine ("\t{0}", t.Name);
		}

		private Test ()
		{
			const string path = "/tmp/TagSelectionTest.db";

			try {
				File.Delete (path);
			} catch {}

			Db db = new Db (path, true);

			Category people_category = db.Tags.CreateCategory (null, "People");
			db.Tags.CreateTag (people_category, "Anna");
			db.Tags.CreateTag (people_category, "Ettore");
			db.Tags.CreateTag (people_category, "Miggy");
			db.Tags.CreateTag (people_category, "Nat");

			Category places_category = db.Tags.CreateCategory (null, "Places");
			db.Tags.CreateTag (places_category, "Milan");
			db.Tags.CreateTag (places_category, "Boston");

			Category exotic_category = db.Tags.CreateCategory (places_category, "Exotic");
			db.Tags.CreateTag (exotic_category, "Bengalore");
			db.Tags.CreateTag (exotic_category, "Manila");
			db.Tags.CreateTag (exotic_category, "Tokyo");

			selection_widget = new TagSelectionWidget (db.Tags);
			selection_widget.SelectionChanged += new SelectionChangedHandler (OnSelectionChanged);

			Window window = new Window (WindowType.Toplevel);
			window.SetDefaultSize (400, 200);
			ScrolledWindow scrolled = new ScrolledWindow (null, null);
			scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
			scrolled.Add (selection_widget);
			window.Add (scrolled);

			window.ShowAll ();
		}

		static private void Main (string [] args)
		{
			Program program = new Program ("TagSelectionWidgetTest", "0.0", Modules.UI, args);

			Test test = new Test ();

			program.Run ();
		}
	}

#endif
}
