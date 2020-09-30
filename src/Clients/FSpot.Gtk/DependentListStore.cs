//
// DependentListStore.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

public class DependentListStore : ListStore
{
	TreeModel parent;

	public TreeModel Parent {
		get { return parent; }
		set {
			if (parent != null) {
				parent.RowInserted -= HandleInserted;
				parent.RowDeleted -= HandleDeleted;
				parent.RowChanged -= HandleChanged;
			}

			parent = value;

			GLib.GType[] types = new GLib.GType[parent.NColumns];
			for (int i = 0; i < parent.NColumns; i++) {
				types[i] = parent.GetColumnType (i);
			}

			this.ColumnTypes = types;

			Copy (parent, this);

			// Listen to the parent to mimick its changes
			parent.RowInserted += HandleInserted;
			parent.RowDeleted += HandleDeleted;
			parent.RowChanged += HandleChanged;
		}
	}

	public DependentListStore (TreeModel tree_model)
	{
		Parent = tree_model;
	}

	/* FIXME: triggering a recopy of the parent doesn't seem to be enough to
	 * get the updated values from it -- at least in the particular case of F-Spot's tag selection widget's model */
	void HandleInserted (object sender, RowInsertedArgs args)
	{
		QueueUpdate ();
	}

	void HandleDeleted (object sender, RowDeletedArgs args)
	{
		QueueUpdate ();
	}

	void HandleChanged (object sender, RowChangedArgs args)
	{
		QueueUpdate ();
	}

	uint timeout_id;
	void QueueUpdate ()
	{
		if (timeout_id != 0)
			GLib.Source.Remove (timeout_id);

		timeout_id = GLib.Timeout.Add (1000, OnUpdateTimer);
	}

	bool OnUpdateTimer ()
	{
		timeout_id = 0;
		Copy (Parent, this);
		return false;
	}

	public static void Copy (TreeModel tree, ListStore list)
	{
		list.Clear ();

		if (tree.IterChildren (out var tree_iter)) {
			Copy (tree, tree_iter, list, true);
		}
	}

	public static void Copy (TreeModel tree, TreeIter tree_iter, ListStore list, bool first)
	{
		// Copy this iter's values to the list
		TreeIter list_iter = list.Append ();
		for (int i = 0; i < list.NColumns; i++) {
			list.SetValue (list_iter, i, tree.GetValue (tree_iter, i));
			if (i == 1) {
				//Console.WriteLine("Copying {0}", list.GetValue(list_iter, i));
			}
		}

		// Copy the first child, which will trigger the copy if its siblings (and their children)
		TreeIter child_iter;
		if (tree.IterChildren (out child_iter, tree_iter)) {
			Copy (tree, child_iter, list, true);
		}

		// Add siblings and their children if we are the first child, otherwise doing so would repeat
		if (first) {
			while (tree.IterNext (ref tree_iter)) {
				Copy (tree, tree_iter, list, false);
			}
		}
	}
}
