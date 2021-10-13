// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FSpot.Database;
using FSpot.Models;
using FSpot.Services;
using FSpot.Settings;
using FSpot.Utils;
using FSpot.Widgets;

using Gdk;

using Gtk;

using Hyena.Widgets;

using Mono.Unix;

namespace FSpot
{
	public class TagSelectionWidget : SaneTreeView
	{
		// FIXME this is a hack.
		static readonly Pixbuf EmptyPixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);

		// If these are changed, the base () call in the constructor must be updated.
		const int IdColumn = 0;
		const int NameColumn = 1;

		static readonly TargetList TagSourceTargetList = new TargetList ();
		static readonly TargetList TagDestTargetList = new TargetList ();

		static TagSelectionWidget ()
		{
			TagSourceTargetList.AddTargetEntry (DragDropTargets.TagListEntry);

			TagDestTargetList.AddTargetEntry (DragDropTargets.PhotoListEntry);
			TagDestTargetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
			TagDestTargetList.AddTargetEntry (DragDropTargets.TagListEntry);
		}

		readonly TreeViewColumn completeColumn;
		readonly CellRendererPixbuf pixbufRender;
		readonly CellRendererText textRender;

		TagStore TagStore { get; }
		TreeStore TagTreeStore { get => Model as TreeStore; }

		protected TagSelectionWidget (IntPtr raw) : base (raw) { }
		public TagSelectionWidget (TagStore tagStore) : base (new TreeStore (typeof (string), typeof (string)))
		{
			HeadersVisible = false;

			completeColumn = new TreeViewColumn ();

			pixbufRender = new CellRendererPixbuf ();
			completeColumn.PackStart (pixbufRender, false);
			completeColumn.SetCellDataFunc (pixbufRender, IconDataFunc);
			//completeColumn.AddAttribute (pixbufRender, "pixbuf", OpenIconColumn);

			//icon_column = AppendColumn ("icon",
			//, new TreeCellDataFunc (IconDataFunc));
			//icon_column = AppendColumn ("icon", new CellRendererPixbuf (), new TreeCellDataFunc (IconDataFunc));

			textRender = new CellRendererText ();
			completeColumn.PackStart (textRender, true);
			completeColumn.SetCellDataFunc (textRender, NameDataFunc);

			AppendColumn (completeColumn);

			TagStore = tagStore;

			Update ();

			ExpandDefaults ();

			TagStore.ItemsAdded += HandleTagsAdded;
			TagStore.ItemsRemoved += HandleTagsRemoved;
			TagStore.ItemsChanged += HandleTagsChanged;

			// TODO make the search find tags that are not currently expanded
			EnableSearch = true;
			SearchColumn = NameColumn;

			// Transparent white
			EmptyPixbuf.Fill (0xffffff00);


			/* set up drag and drop */
			DragDataGet += HandleDragDataGet;
			DragDrop += HandleDragDrop;
			DragBegin += HandleDragBegin;

			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
					   (TargetEntry[])TagSourceTargetList, DragAction.Copy | DragAction.Move);

			DragDataReceived += HandleDragDataReceived;
			DragMotion += HandleDragMotion;

			Gtk.Drag.DestSet (this, DestDefaults.All, (TargetEntry[])TagDestTargetList, DragAction.Copy | DragAction.Move);
		}

		public Tag TagAtPosition (double x, double y)
		{
			return TagAtPosition ((int)x, (int)y);
		}

		public Tag TagAtPosition (int x, int y)
		{
			// Work out which tag we're dropping onto
			if (GetPathAtPos (x, y, out var path))
				return TagByPath (path);

			return null;
		}

		public Tag TagByPath (TreePath path)
		{
			if (Model.GetIter (out var iter, path))
				return TagByIter (iter);

			return null;
		}

		public Tag TagByIter (TreeIter iter)
		{
			var val = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref val);

			return TagStore.Get (Guid.Parse ((string)val));
		}

		// Loading up the store.
		void LoadCategory (Tag category, TreeIter parentIter)
		{ //FIXME, || category.Children.IsEmpty()
			if (category.Children == null)
				TagStore.GetChildren (category);

			var tags = category.Children ?? Enumerable.Empty<Tag> ();

		foreach (Tag t in tags) {
				var iter = TagTreeStore.AppendValues (parentIter, t.Id.ToString (), t.Name);
				if (t.IsCategory)
					LoadCategory (t, iter);
			}
		}

		public void ScrollTo (Tag tag)
		{
			if (!TreeIterForTag (tag, out var iter))
				return;

			TreePath path = Model.GetPath (iter);

			ScrollToCell (path, null, false, 0, 0);
		}

		public IEnumerable<Tag> TagHighlight {
			get {
				TreePath[] rows = Selection.GetSelectedRows (out var model);

				var tags = new List<Tag> (rows.Length);

				foreach (TreePath path in rows) {
					var value = new GLib.Value ();
					Model.GetIter (out var iter, path);
					Model.GetValue (iter, IdColumn, ref value);
					tags.Add (TagStore.Get (Guid.Parse ((string)value)));
				}
				return tags;
			}

			set {
				if (value == null)
					return;

				Selection.UnselectAll ();

				foreach (Tag tag in value)
					if (TreeIterForTag (tag, out var iter))
						Selection.SelectIter (iter);
			}
		}

		public void Update ()
		{
			TagTreeStore.Clear ();

			// GRRR We have to special case the root because I can't pass null for a
			// Gtk.TreeIter (since it's a struct, and not a class).
			// FIXME: This should be fixed in GTK#...  It's gross.

			var rootCategoryChildren = TagStore.RootCategory.Children;
			foreach (Tag t in rootCategoryChildren) {
				TreeIter iter = TagTreeStore.AppendValues (t.Id.ToString (), t.Name);
				if (t.IsCategory)
					LoadCategory (t, iter);
			}
		}

		// Data functions.
		void SetBackground (CellRenderer renderer, Tag tag)
		{
			// FIXME this should be themable but Gtk# doesn't give me access to the proper
			// members in GtkStyle for that.
			/*
			if (tag.IsCategory)
				renderer.CellBackground = ToHashColor (Style.MidColors [(int) Gtk.StateType.Normal]);
			else
				renderer.CellBackground = ToHashColor (Style.LightColors [(int) Gtk.StateType.Normal]);
			*/
		}

		void IconDataFunc (TreeViewColumn column, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			var value = new GLib.Value () ;
			Model.GetValue (iter, IdColumn, ref value);
			Tag tag = TagStore.Get (Guid.Parse((string)value));

			if (tag == null)
				return;

			SetBackground (renderer, tag);

			var icon = EmptyPixbuf;
			if (tag.TagIcon?.SizedIcon != null) {
				if (ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screenProfile)) {
					//FIXME, we're leaking a pixbuf here
					using Pixbuf temp = tag.TagIcon.SizedIcon.Copy ();
					FSpot.ColorManagement.ApplyProfile (temp, screenProfile);
					icon = temp;
				} else {
					icon = tag.TagIcon.SizedIcon;
				}
			}

			(renderer as CellRendererPixbuf).Pixbuf = icon;
		}

		void NameDataFunc (TreeViewColumn column, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			// FIXME not sure why it happens...
			if (model == null)
				return;

			var value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			Tag tag = TagStore.Get (Guid.Parse ((string)value));
			if (tag == null)
				return;

			SetBackground (renderer, tag);

			(renderer as CellRendererText).Text = tag.Name;
		}

		bool TreeIterForTag (Tag tag, out TreeIter iter)
		{
			iter = TreeIter.Zero;
			bool valid = Model.GetIterFirst (out var root);

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

			var value = new GLib.Value ();
			Model.GetValue (parent, IdColumn, ref value);
			iter = parent;

			return tag.Id == Guid.Parse ((string)value);
		}

		// Copy a branch of the tree to a new parent
		// (note, this doesn't work generically as it only copies the first value of each node)
		void CopyBranch (TreeIter src, TreeIter dest, bool isRoot, bool isParent)
		{
			TreeIter copy;
			var store = TagTreeStore;

			var value = new GLib.Value ();
			store.GetValue (src, IdColumn, ref value);
			Tag tag = TagStore.Get (Guid.Parse((string)value));

			if (isParent) {
				// we need to figure out where to insert it in the correct order
				copy = InsertInOrder (dest, isRoot, tag);
			} else {
				copy = store.AppendValues (dest, tag.Id.ToString (), tag.Name);
			}

			var valid = Model.IterChildren (out var iter, src);
			while (valid) {
				// child nodes are already ordered
				CopyBranch (iter, copy, false, false);
				valid = Model.IterNext (ref iter);
			}
		}

		// insert tag into the correct place in the tree, with parent. return the new TagIter in iter.
		TreeIter InsertInOrder (TreeIter parent, bool isRoot, Tag tag)
		{
			var store = TagTreeStore;
			TreeIter iter;
			bool valid;

			if (isRoot)
				valid = store.GetIterFirst (out iter);
			else
				valid = store.IterChildren (out iter, parent);

			var value = new GLib.Value ();
			while (valid) {
				//I have no desire to figure out a more performant sort over this...
				store.GetValue (iter, IdColumn, ref value);
				var compare = TagStore.Get (Guid.Parse((string)value));

				if (compare.CompareTo (tag) > 0) {
					iter = store.InsertNodeBefore (iter);
					store.SetValue (iter, IdColumn, tag.Id.ToString ());
					store.SetValue (iter, NameColumn, tag.Name);

					if (!isRoot)
						ExpandRow (Model.GetPath (parent), false);
					return iter;
				}
				valid = store.IterNext (ref iter);
			}

			if (isRoot)
				iter = store.AppendNode ();
			else {
				iter = store.AppendNode (parent);
				ExpandRow (Model.GetPath (parent), false);
			}

			store.SetValue (iter, IdColumn, tag.Id.ToString ());
			store.SetValue (iter, NameColumn, tag.Name);
			return iter;
		}

		void HandleTagsRemoved (object sender, DbItemEventArgs<Tag> args)
		{
			foreach (Tag tag in args.Items) {
				if (TreeIterForTag (tag, out var iter))
					TagTreeStore.Remove (ref iter);
			}
		}

		void HandleTagsAdded (object sender, DbItemEventArgs<Tag> args)
		{
			TreeIter iter = TreeIter.Zero;

			foreach (Tag tag in args.Items) {
				if (tag.Category != TagStore.RootCategory)
					TreeIterForTag (tag.Category, out iter);

				InsertInOrder (iter, tag.Category.Name == TagStore.RootCategory.Name, tag);
			}
		}

		void HandleTagsChanged (object sender, DbItemEventArgs<Tag> args)
		{
			var store = TagTreeStore;

			foreach (Tag tag in args.Items) {
				TreeIterForTag (tag, out var iter);

				bool categoryValid = TreeIterForTag (tag.Category, out var categoryIter);
				bool parentValid = Model.IterParent (out var parentIter, iter);

				if ((categoryValid && (categoryIter.Equals (parentIter))) || (!categoryValid && !parentValid)) {
					// if we haven't been reparented
					TreePath path = store.GetPath (iter);
					store.EmitRowChanged (path, iter);
				} else {
					// It is a bit tougher. We need to do an annoying clone of structs...
					CopyBranch (iter, categoryIter, !categoryValid, true);
					store.Remove (ref iter);
				}
			}
		}

		void ExpandDefaults ()
		{
			var tags = Preferences.Get<string[]> (Preferences.ExpandedTags);
			if (tags == null) {
				ExpandAll ();
				return;
			}

			TreeIter[] iters = ModelIters ();
			if (iters == null || iters.Length == 0 || tags.Length == 0)
				return;

			var v = new GLib.Value ();
			foreach (TreeIter iter in iters) {
				Model.GetValue (iter, IdColumn, ref v);
				var tagId = Guid.Parse((string)v);
				if (Array.IndexOf (tags, tagId) > -1) {
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
		TreeIter[] ModelIters ()
		{
			if (Model.GetIterFirst (out var root))
				return ModelIters (root, true).ToArray ();

			return null;
		}

		// Returns a List containing the root TreeIter and all TreeIters at root's level and
		// descended from it
		List<TreeIter> ModelIters (TreeIter root, bool first)
		{
			var modelIters = new List<TreeIter> (Model.IterNChildren ()) { root };

			// Append any children
			if (Model.IterChildren (out var child, root))
				modelIters.AddRange (ModelIters (child, true));

			// Append any siblings and their children
			if (first) {
				while (Model.IterNext (ref root)) {
					modelIters.AddRange (ModelIters (root, false));
				}
			}

			return modelIters;
		}

		public void SaveExpandDefaults ()
		{
			var expandedTags = new List<string> ();

			TreeIter[] iters = ModelIters ();
			if (iters == null)
				return;

			var v = new GLib.Value ();
			foreach (TreeIter iter in iters) {
				if (GetRowExpanded (Model.GetPath (iter))) {
					Model.GetValue (iter, IdColumn, ref v);
					expandedTags.Add (v.ToString ());
				}
			}

			Preferences.Set (Preferences.ExpandedTags, expandedTags.ToArray ());
		}

		public void EditSelectedTagName ()
		{
			TreePath[] rows = Selection.GetSelectedRows ();
			if (rows.Length != 1)
				return;

			//SetCursor (rows[0], NameColumn, true);
			textRender.Editable = true;
			textRender.Edited += HandleTagNameEdited;
			SetCursor (rows[0], completeColumn, true);
			textRender.Editable = false;
		}

		public void HandleTagNameEdited (object sender, EditedArgs args)
		{
			args.RetVal = false;

			if (!Model.GetIterFromString (out var iter, args.Path))
				return;

			// FIXME, verify this
			var value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			var tagId = Guid.Parse ((string)value);
			Tag tag = TagStore.Get (tagId);

			// Ignore if it hasn't changed
			if (tag.Name == args.NewText)
				return;

			// Check that the tag doesn't already exist
			if (string.Compare (args.NewText, tag.Name, true) != 0 &&
				TagStore.GetTagByName (args.NewText) != null) {
				using var md = new HigMessageDialog (App.Instance.Organizer.Window,
					DialogFlags.DestroyWithParent, MessageType.Warning, ButtonsType.Ok,
					Catalog.GetString ("Error renaming tag"),
					Catalog.GetString ("This name is already in use"));

				md.Run ();
				md.Destroy ();
				GrabFocus ();
				return;
			}

			tag.Name = args.NewText;
			TagStore.Commit (tag, true);

			textRender.Edited -= HandleTagNameEdited;

			args.RetVal = true;
		}

		void HandleDragBegin (object sender, DragBeginArgs args)
		{
			var tags = TagHighlight.ToArray ();
			int len = tags.Length;
			const int size = 32;
			int csize = size / 2 + len * size / 2 + 2;

			using var container = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, csize, csize);
			container.Fill (0x00000000);

			bool useIcon = false;
			while (len-- > 0) {
				Pixbuf thumbnail = tags[len].TagIcon.Icon;

				if (thumbnail == null)
					continue;

				using Pixbuf small = thumbnail.ScaleToMaxSize (size, size);

				int x = len * (size / 2) + (size - small.Width) / 2;
				int y = len * (size / 2) + (size - small.Height) / 2;

				small.Composite (container, x, y, small.Width, small.Height, x, y, 1.0, 1.0, Gdk.InterpType.Nearest, 0xff);

				useIcon = true;
			}

			if (useIcon)
				Gtk.Drag.SetIconPixbuf (args.Context, container, 0, 0);
		}

		void HandleDragDataGet (object sender, DragDataGetArgs args)
		{
			if (args.Info == DragDropTargets.TagListEntry.Info)
				args.SelectionData.SetTagsData (TagHighlight, args.Context.Targets[0]);
		}

		void HandleDragDrop (object sender, DragDropArgs args)
		{
			args.RetVal = true;
		}

		public void HandleDragMotion (object o, DragMotionArgs args)
		{
			var position = TreeViewDropPosition.IntoOrAfter;
			GetPathAtPos (args.X, args.Y, out var path);

			if (path == null)
				return;

			// Tags can be dropped into another tag
			SetDragDestRow (path, position);

			// Scroll if within 20 pixels of the top or bottom of the tag list
			if (args.Y < 20)
				Vadjustment.Value -= 30;
			else if (((o as Widget).Allocation.Height - args.Y) < 20)
				Vadjustment.Value += 30;
		}

		public void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			if (!GetDestRowAtPos (args.X, args.Y, out var path, out var position))
				return;

			Tag tag = (path == null) ? null : TagByPath (path);
			if (tag == null)
				return;

			if (args.Info == DragDropTargets.PhotoListEntry.Info) {

				var photos = args.SelectionData.GetPhotosData ();

				foreach (var photo in photos) {

					if (photo == null)
						continue;

					TagService.Instance.Add (photo, tag);
					App.Instance.Database.Photos.Commit (photo);

					// FIXME: AddTagExtendes from Mainwindow.cs does some tag-icon handling.
					// this should be done here or completely located to the Tag-class.
				}

				// FIXME: this needs to be done somewhere:
				//query_widget.PhotoTagsChanged (new Tag[] {tag});
				return;
			}

			if (args.Info == (uint)DragDropTargets.TargetType.UriList) {
				UriList list = args.SelectionData.GetUriListData ();

				var photos = new List<Photo> ();
				foreach (var photoUri in list) {
					Photo photo = App.Instance.Database.Photos.GetByUri (photoUri);

					// FIXME - at this point we should import the photo, and then continue
					if (photo == null)
						continue;

					// FIXME this should really follow the AddTagsExtended path too
					TagService.Instance.Add (photo, tag);
					photos.Add (photo);
				}
				App.Instance.Database.Photos.Commit (photos.ToArray ());

				// FIXME: this need to be done
				//InvalidateViews (); // but it seems not to be needed. tags are updated through in IconView through PhotoChanged
				return;
			}

			if (args.Info == DragDropTargets.TagListEntry.Info) {
				Tag parent;
				if (position == TreeViewDropPosition.Before || position == TreeViewDropPosition.After) {
					parent = tag.Category;
				} else {
					parent = tag;
				}

				if (parent == null || !TagHighlight.Any ()) {
					args.RetVal = false;
					return;
				}

				int movedCount = 0;
				var highlightedTags = TagHighlight;
				foreach (Tag child in TagHighlight) {
					// FIXME with this reparenting via dnd, you cannot move a tag to root.
					if (child != parent && child.Category != parent && !child.IsAncestorOf (parent)) {
						child.Category = parent;

						// Saving changes will automatically cause the TreeView to be updated
						App.Instance.Database.Tags.Commit (child);
						movedCount++;
					}
				}

				// Reselect the same tags
				TagHighlight = highlightedTags;

				args.RetVal = movedCount > 0;
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
