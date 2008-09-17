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

using Mono.Unix;
using FSpot;
using FSpot.UI.Dialog;

public class TagSelectionWidget : FSpot.Widgets.SaneTreeView {
	TagSelectionWidget widget;
	private TagStore tag_store;

	// FIXME this is a hack.
	private static Pixbuf empty_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);

	// If these are changed, the base () call in the constructor must be updated.
	private const int IdColumn = 0;
	private const int NameColumn = 1;

	// Selection management.

	public Tag TagAtPosition (double x, double y) 
    {
        return TagAtPosition((int) x, (int) y);
    }

	public Tag TagAtPosition (int x, int y) 
	{
		TreePath path;

		// Work out which tag we're dropping onto
		if (!this.GetPathAtPos (x, y, out path))
			return null;

		return TagByPath (path);
	}

	public Tag TagByPath (TreePath path) 
	{
		TreeIter iter;

		if (!Model.GetIter (out iter, path))
			return null;

		return TagByIter (iter);
	}
	
	public Tag TagByIter (TreeIter iter)
	{
		GLib.Value val = new GLib.Value ();
 
		Model.GetValue (iter, IdColumn, ref val);
		uint tag_id = (uint) val;
 
		return tag_store.Get (tag_id) as Tag;
 	}

	// Loading up the store.

	private void LoadCategory (Category category, TreeIter parent_iter)
	{
		Tag [] tags = category.Children;

		foreach (Tag t in tags) {
			TreeIter iter = (Model as TreeStore).AppendValues (parent_iter, t.Id, t.Name);
			if (t is Category)
				LoadCategory (t as Category, iter);
		}
	}

	public void ScrollTo (Tag tag)
	{
		TreeIter iter;
		if (! TreeIterForTag (tag, out iter))
			return;

		TreePath path = Model.GetPath (iter);

		ScrollToCell (path, null, false, 0, 0);
	}

	public Tag [] TagHighlight {
		get {
			TreeModel model;
			TreeIter iter;

			TreePath [] rows = Selection.GetSelectedRows(out model);

			Tag [] tags = new Tag [rows.Length];
			int i = 0;

			foreach (TreePath path in rows) {
				GLib.Value value = new GLib.Value ();
				Model.GetIter (out iter, path);
				Model.GetValue (iter, IdColumn, ref value);
				uint tag_id = (uint) value;
				tags[i] = tag_store.Get (tag_id) as Tag;
				i++;
			}
			return tags;
		}

		set {
			if (value == null)
				return;

			Selection.UnselectAll ();

			TreeIter iter;
			foreach (Tag tag in value)
				if (TreeIterForTag (tag, out iter))
					Selection.SelectIter (iter);
		}
	}

	public void Update ()
	{
		(Model as TreeStore).Clear ();

		// GRRR We have to special case the root because I can't pass null for a
		// Gtk.TreeIter (since it's a struct, and not a class).
		// FIXME: This should be fixed in GTK#...  It's gross.

		foreach (Tag t in tag_store.RootCategory.Children) {
			TreeIter iter = (Model as TreeStore).AppendValues (t.Id, t.Name);
			if (t is Category)
				LoadCategory (t as Category, iter);
		}
	}

	// Data functions.
	private static string ToHashColor (Gdk.Color color)
	{
		byte r = (byte) (color.Red >> 8);
		byte g = (byte) (color.Green >> 8);
		byte b = (byte) (color.Blue >> 8);
		return String.Format ("#{0:x}{1:x}{2:x}", r, g, b);
	}

	private void SetBackground (CellRenderer renderer, Tag tag)
	{
		// FIXME this should be themable but Gtk# doesn't give me access to the proper
		// members in GtkStyle for that.
		/*
		if (tag is Category)
			renderer.CellBackground = ToHashColor (this.Style.MidColors [(int) Gtk.StateType.Normal]);
		else
			renderer.CellBackground = ToHashColor (this.Style.LightColors [(int) Gtk.StateType.Normal]);
		*/
	}

	private void IconDataFunc (TreeViewColumn column, 
				   CellRenderer renderer,
				   TreeModel model,
				   TreeIter iter)
	{
		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, IdColumn, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		SetBackground (renderer, tag);

		// FIXME I can't set the Pixbuf to null, not sure if it's a GTK# bug...
		if (tag.Icon != null) {
			if (FSpot.ColorManagement.IsEnabled) {
				//FIXME
				Gdk.Pixbuf temp = tag.SizedIcon.Copy();
				FSpot.ColorManagement.ApplyScreenProfile (temp);
				(renderer as CellRendererPixbuf).Pixbuf = temp;
			}
			else
				(renderer as CellRendererPixbuf).Pixbuf = tag.SizedIcon;
		}
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
		Model.GetValue (iter, IdColumn, ref value);
		uint tag_id = (uint) value;

		Tag tag = tag_store.Get (tag_id) as Tag;

		SetBackground (renderer, tag);

		(renderer as CellRendererText).Text = tag.Name;
	}

	private bool TreeIterForTag(Tag tag, out TreeIter iter) 
	{
		TreeIter root = TreeIter.Zero;
		iter = TreeIter.Zero;

		bool valid = Model.GetIterFirst (out root);
		
		while (valid) {
			if (TreeIterForTagRecurse (tag, root, out iter))
				return true;

			valid = Model.IterNext (ref root);
		}
		return false;
	}

	// Depth first traversal
	private bool TreeIterForTagRecurse (Tag tag, TreeIter parent, out TreeIter iter) 
	{
		bool valid = Model.IterChildren (out iter, parent);

		while (valid) {
			if (TreeIterForTagRecurse (tag, iter, out iter))
				return true;
			valid = Model.IterNext (ref iter);
		}

		GLib.Value value = new GLib.Value ();
		Model.GetValue (parent, IdColumn, ref value);
		iter = parent;

		if (tag.Id == (uint) value)
			return true;

		return false;
	}
	
	// Copy a branch of the tree to a new parent
	// (note, this doesn't work generically as it only copies the first value of each node)
	private void CopyBranch (TreeIter src, TreeIter dest, bool is_root, bool is_parent) 
	{
		TreeIter copy, iter;
		GLib.Value value = new GLib.Value ();
		TreeStore store = Model as TreeStore;
		bool valid;
		
		store.GetValue (src, IdColumn, ref value);
		Tag tag = (Tag) tag_store.Get ((uint)value);
		if (is_parent) {
			// we need to figure out where to insert it in the correct order
			copy = InsertInOrder(dest, is_root, tag);
		} else { 
			copy = store.AppendValues (dest, (uint)value, tag.Name);
		}
		
		valid = Model.IterChildren (out iter, src);
		while (valid) {
			// child nodes are already ordered
			CopyBranch (iter, copy, false, false);
			valid = Model.IterNext (ref iter);
		}
	}

	// insert tag into the correct place in the tree, with parent. return the new TagIter in iter.
	private TreeIter InsertInOrder (TreeIter parent, bool is_root, Tag tag) 
	{
		TreeStore store = Model as TreeStore;
		TreeIter iter;
		Tag compare;
		bool valid;

		if (is_root)
			valid = store.GetIterFirst (out iter);
		else
			valid = store.IterChildren (out iter, parent);

		while (valid) {
			//I have no desire to figure out a more performant sort over this...
			GLib.Value value = new GLib.Value ();
			store.GetValue(iter, IdColumn, ref value);
			compare = (Tag) tag_store.Get ((uint) value);

			if (compare.CompareTo (tag) > 0) {
				iter = store.InsertNodeBefore (iter);
				store.SetValue (iter, IdColumn, tag.Id);
				store.SetValue (iter, NameColumn, tag.Name);
				
				if (!is_root)
					ExpandRow (Model.GetPath (parent), false);
				return iter;
			}
			valid = store.IterNext(ref iter);
		}
		
		if (is_root) 
			iter = store.AppendNode (); 
		else {
			iter = store.AppendNode (parent); 
			ExpandRow (Model.GetPath (parent), false);
		}

		store.SetValue (iter, IdColumn, tag.Id);
		store.SetValue (iter, NameColumn, tag.Name);
		return iter;
	}

	private void HandleTagsRemoved (object sender, DbItemEventArgs args)
	{
		TreeIter iter;

		foreach (DbItem item in args.Items) {
			Tag tag = (Tag)item;

			if (TreeIterForTag (tag, out iter)) 
				(Model as TreeStore).Remove (ref iter);
		}
	}
	
	private void HandleTagsAdded (object sender, DbItemEventArgs args)
	{
		TreeIter iter = TreeIter.Zero;
		
		foreach (DbItem item in args.Items) {
			Tag tag = (Tag)item;

			if (tag.Category != tag_store.RootCategory)
				TreeIterForTag (tag.Category, out iter);

			InsertInOrder (iter,
				       tag.Category.Name == tag_store.RootCategory.Name,
				       tag);
		}
	}
	
	private void HandleTagsChanged (object sender, DbItemEventArgs args)
	{
		TreeStore store = Model as TreeStore;
		TreeIter iter, category_iter, parent_iter;

		foreach (DbItem item in args.Items) {
			Tag tag = (Tag) item;

			TreeIterForTag (tag, out iter);
			
			bool category_valid = TreeIterForTag(tag.Category, out category_iter);
			bool parent_valid = Model.IterParent(out parent_iter, iter);
			
			if ((category_valid && (category_iter.Equals (parent_iter))) || (!category_valid && !parent_valid)) {
				// if we haven't been reparented
				TreePath path = store.GetPath (iter); 
				store.EmitRowChanged (path, iter);
			} else {
				// It is a bit tougher. We need to do an annoying clone of structs...
				CopyBranch (iter, category_iter, !category_valid, true);
				store.Remove (ref iter);
			}
		}
	}

	void ExpandDefaults ()
	{
		int [] tags = FSpot.Preferences.Get<int []> (FSpot.Preferences.EXPANDED_TAGS);
		if (tags == null) {
			ExpandAll ();
			return;
		}

		TreeIter [] iters = ModelIters ();
		if (iters == null || iters.Length == 0 || tags.Length == 0)
			return;

		foreach (TreeIter iter in iters)
		{
			GLib.Value v = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref v);
			int tag_id = (int)(uint) v;
			if (Array.IndexOf (tags, tag_id) > -1) {
				ExpandRow (Model.GetPath (iter), false);
			}
		}
	}

	// Returns a flattened array of TreeIter's from the Model
	TreeIter [] ModelIters ()
	{
		TreeIter root;
		if (Model.GetIterFirst (out root))
		{
			return ModelIters (root, true).ToArray (typeof (TreeIter)) as TreeIter [];
		}

		return null;
	}

	// Returns ArrayList containing the root TreeIter and all TreeIters at root's level and
	// descended from it
	ArrayList ModelIters (TreeIter root, bool first)
	{
		ArrayList model_iters = new ArrayList (Model.IterNChildren ());

		model_iters.Add (root);

		// Append any children
		TreeIter child;
		if (Model.IterChildren (out child, root))
			model_iters.AddRange (ModelIters (child, true));
		
		// Append any siblings and their children
		if (first) {
			while (Model.IterNext (ref root)) {
				model_iters.AddRange (ModelIters (root, false));
			}
		}

		return model_iters;
	}

	public void SaveExpandDefaults ()
	{
		ArrayList expanded_tags = new ArrayList ();
		
		TreeIter [] iters = ModelIters ();
		if (iters == null)
			return;

		foreach (TreeIter iter in iters)
		{
			if (GetRowExpanded (Model.GetPath (iter))) {
				GLib.Value v = new GLib.Value ();
				Model.GetValue (iter, IdColumn, ref v);
				expanded_tags.Add ((int)(uint) v);
			}
		}

#if GCONF_SHARP_2_18
		FSpot.Preferences.Set (	FSpot.Preferences.EXPANDED_TAGS, (int []) expanded_tags.ToArray (typeof (int)));
#else
		if (expanded_tags.Count == 0)
			expanded_tags.Add (-1);

		FSpot.Preferences.Set (	FSpot.Preferences.EXPANDED_TAGS,
						(int []) expanded_tags.ToArray (typeof (int)));
#endif
	}

	public void EditSelectedTagName ()
	{
		TreePath [] rows = Selection.GetSelectedRows();
		if (rows.Length != 1)
			return;
		
		//SetCursor (rows[0], NameColumn, true);
		text_render.Editable = true;
		text_render.Edited += HandleTagNameEdited;
		SetCursor (rows[0], complete_column, true);
		text_render.Editable = false;
	}

	public void HandleTagNameEdited (object sender, EditedArgs args)
	{
		args.RetVal = false;

		TreeIter iter;

		if (!Model.GetIterFromString (out iter, args.Path))
			return;

		GLib.Value value = new GLib.Value ();
		Model.GetValue (iter, IdColumn, ref value);
		uint tag_id = (uint) value;
		Tag tag = tag_store.Get (tag_id) as Tag;

		// Ignore if it hasn't changed
		if (tag.Name == args.NewText)
			return;

		// Check that the tag doesn't already exist
		if (String.Compare (args.NewText, tag.Name, true) != 0 &&
		    tag_store.GetTagByName (args.NewText) != null) {
			HigMessageDialog md = new HigMessageDialog (MainWindow.Toplevel.Window,
				DialogFlags.DestroyWithParent, 
				MessageType.Warning, ButtonsType.Ok, 
				Catalog.GetString ("Error renaming tag"),
				Catalog.GetString ("This name is already in use"));

			md.Run ();
			md.Destroy ();
			this.GrabFocus ();
			return;
		}

		tag.Name = args.NewText;
		tag_store.Commit (tag, true);

		text_render.Edited -= HandleTagNameEdited;

		args.RetVal = true;
		return;
	}

	//TreeViewColumn check_column;
	//TreeViewColumn icon_column;
	//TreeViewColumn name_column;
	CellRendererPixbuf pix_render;
	TreeViewColumn complete_column;
	CellRendererText text_render;

	// Constructor.
	public TagSelectionWidget (TagStore tag_store)
		: base (new TreeStore (typeof(uint), typeof(string)))
	{
		HeadersVisible = false;

		complete_column = new TreeViewColumn ();
				
		pix_render = new CellRendererPixbuf ();
		complete_column.PackStart (pix_render, false);
		complete_column.SetCellDataFunc (pix_render, new TreeCellDataFunc (IconDataFunc));
		//complete_column.AddAttribute (pix_render, "pixbuf", OpenIconColumn);

		//icon_column = AppendColumn ("icon", 
		//, new TreeCellDataFunc (IconDataFunc));
		//icon_column = AppendColumn ("icon", new CellRendererPixbuf (), new TreeCellDataFunc (IconDataFunc));

		text_render = new CellRendererText ();
		complete_column.PackStart (text_render, true);
		complete_column.SetCellDataFunc (text_render, new TreeCellDataFunc (NameDataFunc));

		AppendColumn (complete_column);

		this.tag_store = tag_store;

		Update ();

		ExpandDefaults ();

		tag_store.ItemsAdded += HandleTagsAdded;
		tag_store.ItemsRemoved += HandleTagsRemoved;
		tag_store.ItemsChanged += HandleTagsChanged;

		// TODO make the search find tags that are not currently expanded
		EnableSearch = true;
		SearchColumn = NameColumn;

		// Transparent white
		empty_pixbuf.Fill(0xffffff00);
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
