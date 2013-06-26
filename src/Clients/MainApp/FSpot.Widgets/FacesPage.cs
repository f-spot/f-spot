//
// EditorPage.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//   Mike Gemuende <mike@gemuende.de>
//   Valentín Barros <valentin@sanva.net>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2008-2010 Stephane Delcroix
// Copyright (C) 2009 Mike Gemuende
// Copyright (C) 2013 Valentín Barros
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
using System.Collections.Generic;

using FSpot.Database;
using FSpot.Extensions;
using FSpot.Core;

using Gdk;

using Gtk;

using Mono.Addins;
using Mono.Unix;

namespace FSpot.Widgets {
	public class FacesPage : SidebarPage {
		internal bool InPhotoView;
		private readonly FacesPageWidget FacesPageWidget;

		public FacesPage () : base (new FacesPageWidget (), Catalog.GetString ("Faces"), "gtk-missing-image") {
			// TODO: Somebody might need to change the icon to something more suitable.
			// FIXME: The icon isn't shown in the menu, are we missing a size?
			// TODO Obviously, the icon is something temporal, we would need an icon for Faces.
			FacesPageWidget = SidebarWidget as FacesPageWidget;
			FacesPageWidget.Page = this;
		}

		protected override void AddedToSidebar () {
			// TODO Need to do something here?
		}
	}

	public class FacesPageWidget : Gtk.ScrolledWindow {
		private Box widgets;

		private FacesPage page;
		internal FacesPage Page { get; set; }
		
		public FacesPageWidget ()
		{
			widgets = new VBox (false, 0);
			widgets.Add (new FaceSelectionWidget (App.Instance.Database.Faces));
			Viewport widgets_port = new Viewport ();
			widgets_port.Add (widgets);
			Add (widgets_port);
			widgets_port.ShowAll ();
		}
	}

	public class FaceSelectionWidget : TreeView {
		
		Db database;
		FaceStore face_store;
		
		// FIXME this is a hack.
		private static Pixbuf empty_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);
		
		// If these are changed, the base () call in the constructor must be updated.
		private const int IdColumn = 0;
		private const int NameColumn = 1;

		public void ScrollTo (Face face)
		{
			TreeIter iter;
			if (! TreeIterForFace (face, out iter))
				return;
			
			TreePath path = Model.GetPath (iter);
			
			ScrollToCell (path, null, false, 0, 0);
		}
		
		public Face [] FaceHighlight {
			get {
				TreeModel model;
				TreeIter iter;
				
				TreePath [] rows = Selection.GetSelectedRows(out model);
				
				Face [] faces = new Face [rows.Length];
				int i = 0;
				
				foreach (TreePath path in rows) {
					GLib.Value value = new GLib.Value ();
					Model.GetIter (out iter, path);
					Model.GetValue (iter, IdColumn, ref value);
					uint face_id = (uint) value;
					faces[i] = face_store.Get (face_id);
					i++;
				}
				return faces;
			}
			
			set {
				if (value == null)
					return;
				
				Selection.UnselectAll ();
				
				TreeIter iter;
				foreach (Face face in value)
					if (TreeIterForFace (face, out iter))
						Selection.SelectIter (iter);
			}
		}
		
		public void Update ()
		{
			TreeStore store = Model as TreeStore;
			store.Clear ();

			IEnumerable<Face> faces = face_store.GetAll ();
			foreach (Face face in faces) {Console.Out.WriteLine (face.Name);
				store.AppendValues (face.Id, face.Name);
			}
		}
		
		// Data functions.

		private void IconDataFunc (TreeViewColumn column,
		                           CellRenderer renderer,
		                           TreeModel model,
		                           TreeIter iter)
		{
			GLib.Value value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			uint face_id = (uint) value;
			Face face = face_store.Get (face_id);
			
			if (face == null)
				return;

			if (face.SizedIcon != null) {
				Cms.Profile screen_profile;
				if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
					//FIXME, we're leaking a pixbuf here
					using (Gdk.Pixbuf temp = face.SizedIcon.Copy ()) {
						FSpot.ColorManagement.ApplyProfile (temp, screen_profile);
						(renderer as CellRendererPixbuf).Pixbuf = temp;
					}
				} else
					(renderer as CellRendererPixbuf).Pixbuf = face.SizedIcon;
			} else
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
			uint face_id = (uint) value;
			
			Face face = face_store.Get (face_id);
			if (face == null)
				return;
			
			(renderer as CellRendererText).Text = face.Name;
		}
		
		private bool TreeIterForFace(Face face, out TreeIter iter)
		{
			TreeIter root = TreeIter.Zero;
			iter = TreeIter.Zero;
			
			bool valid = Model.GetIterFirst (out root);
			
			while (valid) {
				if (TreeIterForFaceRecurse (face, root, out iter))
					return true;
				
				valid = Model.IterNext (ref root);
			}
			return false;
		}
		
		// Depth first traversal
		private bool TreeIterForFaceRecurse (Face face, TreeIter parent, out TreeIter iter)
		{
			bool valid = Model.IterChildren (out iter, parent);
			
			while (valid) {
				if (TreeIterForFaceRecurse (face, iter, out iter))
					return true;
				valid = Model.IterNext (ref iter);
			}
			
			GLib.Value value = new GLib.Value ();
			Model.GetValue (parent, IdColumn, ref value);
			iter = parent;
			
			if (face.Id == (uint) value)
				return true;
			
			return false;
		}
		
		// insert tag into the correct place in the tree, with parent. return the new TagIter in iter.
		private TreeIter InsertInOrder (Face face)
		{
			TreeStore store = Model as TreeStore;
			TreeIter iter;
			Face compare;
			bool valid;

			valid = store.GetIterFirst (out iter);

			while (valid) {
				GLib.Value value = new GLib.Value ();
				store.GetValue(iter, IdColumn, ref value);
				compare = face_store.Get ((uint) value);
				
				if (compare.CompareTo (face) > 0) {
					iter = store.InsertNodeBefore (iter);
					store.SetValue (iter, IdColumn, face.Id);
					store.SetValue (iter, NameColumn, face.Name);

					return iter;
				}
				valid = store.IterNext(ref iter);
			}

			iter = store.AppendNode ();
			store.SetValue (iter, IdColumn, face.Id);
			store.SetValue (iter, NameColumn, face.Name);

			return iter;
		}
		
		private void HandleFacesRemoved (object sender, DbItemEventArgs<Face> args)
		{
			TreeIter iter;
			TreeStore store = Model as TreeStore;
			foreach (Face face in args.Items)
				if (TreeIterForFace (face, out iter))
					store.Remove (ref iter);
		}
		
		private void HandleFacesAdded (object sender, DbItemEventArgs<Face> args)
		{
			foreach (Face face in args.Items)
				InsertInOrder (face);
		}
		
		private void HandleFacesChanged (object sender, DbItemEventArgs<Face> args)
		{
			// TODO Not sure if I should do something here... in TagsPage it seems
			// that the thing is about the possibility of moving the Tag from one
			// parent to another... but Faces are a list, not a tree —but it could
			// be necessary to handle a renaming event.
		}
		
		public void EditSelectedFaceName ()
		{
			TreePath [] rows = Selection.GetSelectedRows();
			if (rows.Length != 1)
				return;

			text_render.Editable = true;
			text_render.Edited += HandleFaceNameEdited;
			SetCursor (rows[0], complete_column, true);
			text_render.Editable = false;
		}
		
		public void HandleFaceNameEdited (object sender, EditedArgs args)
		{
			args.RetVal = false;
			
			TreeIter iter;
			
			if (!Model.GetIterFromString (out iter, args.Path))
				return;
			
			GLib.Value value = new GLib.Value ();
			Model.GetValue (iter, IdColumn, ref value);
			uint face_id = (uint) value;
			Face face = face_store.Get (face_id);
			
			// Ignore if it hasn't changed
			if (face.Name == args.NewText)
				return;
			
			face.Name = args.NewText;
			face_store.Commit (face, true);
			
			text_render.Edited -= HandleFaceNameEdited;
			
			args.RetVal = true;
		}
		
		CellRendererPixbuf pix_render;
		TreeViewColumn complete_column;
		CellRendererText text_render;
		
		protected FaceSelectionWidget (IntPtr raw) : base (raw) { }
		
		// Constructor.
		public FaceSelectionWidget (FaceStore face_store)
			: base (new TreeStore (typeof(uint), typeof(string)))
		{
			database = App.Instance.Database;
			
			HeadersVisible = false;
			
			complete_column = new TreeViewColumn ();
			
			pix_render = new CellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.SetCellDataFunc (pix_render, new TreeCellDataFunc (IconDataFunc));

			text_render = new CellRendererText ();
			complete_column.PackStart (text_render, true);
			complete_column.SetCellDataFunc (text_render, new TreeCellDataFunc (NameDataFunc));
			
			AppendColumn (complete_column);
			
			this.face_store = face_store;
			
			Update ();
			
			face_store.ItemsAdded += HandleFacesAdded;
			face_store.ItemsRemoved += HandleFacesRemoved;
			face_store.ItemsChanged += HandleFacesChanged;

			EnableSearch = true;
			SearchColumn = NameColumn;
			
			// Transparent white
			empty_pixbuf.Fill (0xffffff00);
		}
	}
}
