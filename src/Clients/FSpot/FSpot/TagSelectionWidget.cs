//
// TagSelectionWidget.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Mike Gemuende <mike@gemuende.de>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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

using System.Collections.Generic;
using System;

using Gdk;

using Gtk;

using Mono.Unix;

using FSpot.Core;
using FSpot.Database;
using FSpot.Settings;
using FSpot.Utils;
using FSpot.Widgets;

using Hyena.Widgets;



namespace FSpot
{
	public class TagSelectionWidget : SaneTreeView
	{
		readonly Db database;
		TagStore tag_store;

		// FIXME this is a hack.
		static Pixbuf empty_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);

		// If these are changed, the base () call in the constructor must be updated.
		const int IdColumn = 0;
		const int NameColumn = 1;

		// Selection management.

		public Tag TagAtPosition (double x, double y)
		{
			return TagAtPosition((int) x, (int) y);
		}

		public Tag TagAtPosition (int x, int y)
		{
			TreePath path;

			// Work out which tag we're dropping onto
			if (!GetPathAtPos (x, y, out path))
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

			return tag_store.Get (tag_id);
		}

		// Loading up the store.
		void LoadCategory (Category category, TreeIter parent_iter)
		{
			IList<Tag> tags = category.Children;

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
					tags[i] = tag_store.Get (tag_id);
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
		void SetBackground (CellRenderer renderer, Tag tag)
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

		void IconDataFunc (TreeViewColumn column, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			GLib.Value value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			uint tag_id = (uint) value;
			Tag tag = tag_store.Get (tag_id);

			if (tag == null)
				return;

			SetBackground (renderer, tag);

			if (tag.SizedIcon != null) {
				Cms.Profile screen_profile;
				if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
					//FIXME, we're leaking a pixbuf here
					using (Gdk.Pixbuf temp = tag.SizedIcon.Copy ()) {
						FSpot.ColorManagement.ApplyProfile (temp, screen_profile);
						(renderer as CellRendererPixbuf).Pixbuf = temp;
					}
				} else
					(renderer as CellRendererPixbuf).Pixbuf = tag.SizedIcon;
			} else
				(renderer as CellRendererPixbuf).Pixbuf = empty_pixbuf;
		}

		void NameDataFunc (TreeViewColumn column, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			// FIXME not sure why it happens...
			if (model == null)
				return;

			GLib.Value value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			uint tag_id = (uint) value;

			Tag tag = tag_store.Get (tag_id);
			if (tag == null)
				return;

			SetBackground (renderer, tag);

			(renderer as CellRendererText).Text = tag.Name;
		}

		bool TreeIterForTag(Tag tag, out TreeIter iter)
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
		bool TreeIterForTagRecurse (Tag tag, TreeIter parent, out TreeIter iter)
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
		void CopyBranch (TreeIter src, TreeIter dest, bool is_root, bool is_parent)
		{
			TreeIter copy, iter;
			GLib.Value value = new GLib.Value ();
			TreeStore store = Model as TreeStore;
			bool valid;

			store.GetValue (src, IdColumn, ref value);
			Tag tag = tag_store.Get ((uint)value);
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
		TreeIter InsertInOrder (TreeIter parent, bool is_root, Tag tag)
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
				compare = tag_store.Get ((uint) value);

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

		void HandleTagsRemoved (object sender, DbItemEventArgs<Tag> args)
		{
			TreeIter iter;

			foreach (Tag tag in args.Items) {
				if (TreeIterForTag (tag, out iter))
					(Model as TreeStore).Remove (ref iter);
			}
		}

		void HandleTagsAdded (object sender, DbItemEventArgs<Tag> args)
		{
			TreeIter iter = TreeIter.Zero;

			foreach (Tag tag in args.Items) {
				if (tag.Category != tag_store.RootCategory)
					TreeIterForTag (tag.Category, out iter);

				InsertInOrder (iter,
					       tag.Category.Name == tag_store.RootCategory.Name,
					       tag);
			}
		}

		void HandleTagsChanged (object sender, DbItemEventArgs<Tag> args)
		{
			TreeStore store = Model as TreeStore;
			TreeIter iter, category_iter, parent_iter;

			foreach (Tag tag in args.Items) {
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
			int [] tags = Preferences.Get<int []> (Preferences.EXPANDED_TAGS);
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

		/// <summary>
		/// Returns a flattened array of TreeIter's from the Model
		/// </summary>
		/// <returns>
		/// TreeIter []
		/// </returns>
		TreeIter [] ModelIters ()
		{
			TreeIter root;
			if (Model.GetIterFirst (out root))
			{
				return ModelIters (root, true).ToArray ();
			}
			return null;
		}

		// Returns a List containing the root TreeIter and all TreeIters at root's level and
		// descended from it
		List<TreeIter> ModelIters (TreeIter root, bool first)
		{
			List<TreeIter> model_iters = new List<TreeIter> (Model.IterNChildren ());
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
			List<int> expanded_tags = new List<int> ();

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
			FSpot.Preferences.Set (	FSpot.Preferences.EXPANDED_TAGS, expanded_tags.ToArray ());
	#else
			if (expanded_tags.Count == 0)
				expanded_tags.Add (-1);

			Preferences.Set (Preferences.EXPANDED_TAGS, expanded_tags.ToArray ());
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
			Tag tag = tag_store.Get (tag_id);

			// Ignore if it hasn't changed
			if (tag.Name == args.NewText)
				return;

			// Check that the tag doesn't already exist
			if (string.Compare (args.NewText, tag.Name, true) != 0 &&
			    tag_store.GetTagByName (args.NewText) != null) {
				HigMessageDialog md = new HigMessageDialog (App.Instance.Organizer.Window,
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
		}

        static TargetList tagSourceTargetList = new TargetList();
        static TargetList tagDestTargetList = new TargetList();

        static TagSelectionWidget()
        {
            tagSourceTargetList.AddTargetEntry(DragDropTargets.TagListEntry);

            tagDestTargetList.AddTargetEntry(DragDropTargets.PhotoListEntry);
            tagDestTargetList.AddUriTargets((uint)DragDropTargets.TargetType.UriList);
            tagDestTargetList.AddTargetEntry(DragDropTargets.TagListEntry);
        }

		CellRendererPixbuf pix_render;
		TreeViewColumn complete_column;
		CellRendererText text_render;

		protected TagSelectionWidget (IntPtr raw) : base (raw) { }

		// Constructor.
		public TagSelectionWidget (TagStore tag_store)
			: base (new TreeStore (typeof(uint), typeof(string)))
		{
			database = App.Instance.Database;

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


			/* set up drag and drop */
			DragDataGet += HandleDragDataGet;
			DragDrop += HandleDragDrop;
			DragBegin += HandleDragBegin;

			Gtk.Drag.SourceSet (this,
			           Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
			           (TargetEntry[])tagSourceTargetList,
			           DragAction.Copy | DragAction.Move);

			DragDataReceived += HandleDragDataReceived;
			DragMotion += HandleDragMotion;

			Gtk.Drag.DestSet (this,
			                  DestDefaults.All,
			                  (TargetEntry[])tagDestTargetList,
			                  DragAction.Copy | DragAction.Move);
		}

		void HandleDragBegin (object sender, DragBeginArgs args)
		{
			Tag [] tags = TagHighlight;
			int len = tags.Length;
			int size = 32;
			int csize = size/2 + len * size / 2 + 2;

			Pixbuf container = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, csize, csize);
			container.Fill (0x00000000);

			bool use_icon = false;;
			while (len-- > 0) {
				Pixbuf thumbnail = tags[len].Icon;

				if (thumbnail != null) {
					Pixbuf small = thumbnail.ScaleToMaxSize (size, size);

					int x = len * (size/2) + (size - small.Width)/2;
					int y = len * (size/2) + (size - small.Height)/2;

					small.Composite (container, x, y, small.Width, small.Height, x, y, 1.0, 1.0, Gdk.InterpType.Nearest, 0xff);
					small.Dispose ();

					use_icon = true;
				}
			}

			if (use_icon)
				Gtk.Drag.SetIconPixbuf (args.Context, container, 0, 0);

			container.Dispose ();
		}

		void HandleDragDataGet (object sender, DragDataGetArgs args)
		{
			if (args.Info == DragDropTargets.TagListEntry.Info) {
				args.SelectionData.SetTagsData (TagHighlight, args.Context.Targets[0]);
			}
		}

		void HandleDragDrop (object sender, DragDropArgs args)
		{
			args.RetVal = true;
		}

		public void HandleDragMotion (object o, DragMotionArgs args)
		{
			TreePath path;
	        TreeViewDropPosition position = TreeViewDropPosition.IntoOrAfter;
			GetPathAtPos (args.X, args.Y, out path);

	        if (path == null)
	            return;

	        // Tags can be dropped into another tag
			SetDragDestRow (path, position);

			// Scroll if within 20 pixels of the top or bottom of the tag list
			if (args.Y < 20)
				Vadjustment.Value -= 30;
	        else if (((o as Gtk.Widget).Allocation.Height - args.Y) < 20)
				Vadjustment.Value += 30;
		}

		public void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
	        TreePath path;
	        TreeViewDropPosition position;

	        if ( ! GetDestRowAtPos ((int)args.X, (int)args.Y, out path, out position))
	            return;

	        Tag tag = path == null ? null : TagByPath (path);
			if (tag == null)
				return;

			if (args.Info == DragDropTargets.PhotoListEntry.Info) {
				database.BeginTransaction ();

				Photo[] photos = args.SelectionData.GetPhotosData ();

				foreach (Photo photo in photos) {

					if (photo == null)
						continue;

					photo.AddTag (tag);
					database.Photos.Commit (photo);

					// FIXME: AddTagExtendes from Mainwindow.cs does some tag-icon handling.
					// this should be done here or completely located to the Tag-class.
				}
				database.CommitTransaction ();

				// FIXME: this needs to be done somewhere:
				//query_widget.PhotoTagsChanged (new Tag[] {tag});
				return;
			}

			if (args.Info == (uint)DragDropTargets.TargetType.UriList) {
				UriList list = args.SelectionData.GetUriListData ();

				database.BeginTransaction ();
				List<Photo> photos = new List<Photo> ();
				foreach (var photo_uri in list) {
					Photo photo = database.Photos.GetByUri (photo_uri);

					// FIXME - at this point we should import the photo, and then continue
					if (photo == null)
						continue;

					// FIXME this should really follow the AddTagsExtended path too
					photo.AddTag (new Tag[] {tag});
					photos.Add (photo);
				}
				database.Photos.Commit (photos.ToArray ());
				database.CommitTransaction ();

				// FIXME: this need to be done
				//InvalidateViews (); // but it seems not to be needed. tags are updated through in IconView through PhotoChanged
				return;
			}

			if (args.Info == DragDropTargets.TagListEntry.Info) {
				Category parent;
	            if (position == TreeViewDropPosition.Before || position == TreeViewDropPosition.After) {
	                parent = tag.Category;
	            } else {
	                parent = tag as Category;
	            }

				if (parent == null || TagHighlight.Length < 1) {
	                args.RetVal = false;
					return;
	            }

	            int moved_count = 0;
	            Tag [] highlighted_tags = TagHighlight;
				foreach (Tag child in TagHighlight) {
	                // FIXME with this reparenting via dnd, you cannot move a tag to root.
	                if (child != parent && child.Category != parent && !child.IsAncestorOf(parent)) {
	                    child.Category = parent;

	                    // Saving changes will automatically cause the TreeView to be updated
	                    database.Tags.Commit (child);
	                    moved_count++;
	                }
	            }

	            // Reselect the same tags
	            TagHighlight = highlighted_tags;

	            args.RetVal = moved_count > 0;
			}
		}


	#if TEST_TAG_SELECTION_WIDGET

		class Test {

			private TagSelectionWidget selection_widget;

			private void OnSelectionChanged ()
			{
				Log.Debug ("Selection changed:");

				foreach (Tag t in selection_widget.TagSelection)
					Log.DebugFormat ("\t{0}", t.Name);
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
}
