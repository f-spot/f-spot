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

using Hyena.Widgets;

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

		public FacesPage ()
			: base (FacesPageWidget.Instance, Catalog.GetString ("Faces"), "gtk-missing-image")
		{
			// TODO: Somebody might need to change the icon to something more suitable.
			// FIXME: The icon isn't shown in the menu, are we missing a size?
			// TODO Obviously, the icon is something temporal, we would need an icon for Faces.
			FacesPageWidget = SidebarWidget as FacesPageWidget;
			FacesPageWidget.Page = this;
		}

		protected override void AddedToSidebar ()
		{
			Sidebar sidebar = Sidebar as Sidebar;
			sidebar.ContextChanged += HandleContextChanged;
			sidebar.PageSwitched += FacesPageWidget.HandleSidebarPageSwitched;
		}

		private void HandleContextChanged (object sender, EventArgs args)
		{
			InPhotoView = ((Sidebar as Sidebar).Context == ViewContext.Edit);

			FacesPageWidget.HandleContextChanged ();
		}
	}

	public class FacesPageWidget : VBox {
		private static FacesPageWidget instance = null;
		public static FacesPageWidget Instance {
			get {
				if (instance == null)
					instance = new FacesPageWidget();
				
				return instance;
			}
		}

		public FaceSelectionWidget FacesWidget;

		private FacesTool faces_tool;
		private Gtk.ScrolledWindow face_selection_scrolled_window;
		private Gtk.ScrolledWindow faces_tool_scrolled_window;
		private Gtk.Viewport faces_tool_viewport = null;
		private WeakReference loaded_photo_ref;

		internal FacesPage Page { get; set; }
		
		private FacesPageWidget ()
			: base (false, 0)
		{
			loaded_photo_ref = new WeakReference (null);

			FacesWidget = new FaceSelectionWidget (App.Instance.Database.Faces);
			Viewport face_selection_viewport = new Viewport ();
			face_selection_viewport.Add (FacesWidget);
			face_selection_scrolled_window = new Gtk.ScrolledWindow ();
			face_selection_scrolled_window.Add (face_selection_viewport);

			PackStart (face_selection_scrolled_window, true, true, 0);

			ShowAll ();
		}

		private void ShowFacesTool ()
		{
			if (faces_tool_viewport == null) {
				faces_tool_viewport = new Viewport ();

				faces_tool_scrolled_window = new Gtk.ScrolledWindow ();
				faces_tool_scrolled_window.Add (faces_tool_viewport);

				PackStart (faces_tool_scrolled_window, true, true, 0);
			}

			faces_tool_viewport.Add (faces_tool.Window);

			face_selection_scrolled_window.Hide ();
			faces_tool_scrolled_window.ShowAll ();
		}

		private void HideFacesTool ()
		{
			if (faces_tool_scrolled_window != null)
				faces_tool_scrolled_window.Hide ();

			face_selection_scrolled_window.ShowAll ();
		}

		public void HandleSidebarPageSwitched (object sender, EventArgs args)
		{
			if (faces_tool == null)
				return;

			Sidebar sidebar = Page.Sidebar as Sidebar;
			if (sidebar.IsActive (Page)) {
				faces_tool.Activate ();

				ShowFacesTool ();
			} else
				faces_tool.Deactivate ();
		}

		public void HandleContextChanged ()
		{
			PhotoView photo_view = App.Instance.Organizer.PhotoView;
			if (Page.InPhotoView) {
				IPhoto loaded_photo = loaded_photo_ref.Target as IPhoto;
				if (loaded_photo != photo_view.Item.Current) {
					loaded_photo_ref.Target = photo_view.Item.Current;

					photo_view.View.PhotoLoaded += OnPhotoLoaded;
				} else
					photo_view.View.SizeAllocated += OnPhotoSizeAllocated;

				photo_view.PhotoChanged += OnPhotoChanged;
			} else {
				photo_view.PhotoChanged -= OnPhotoChanged;

				if (faces_tool == null)
					return;

				faces_tool.Dispose ();
				faces_tool = null;

				HideFacesTool ();
			}
		}

		void OnPhotoSizeAllocated (object sender, SizeAllocatedArgs e)
		{
			((PhotoImageView) sender).SizeAllocated -= OnPhotoSizeAllocated;

			if (faces_tool != null)
				faces_tool.Dispose ();

			faces_tool = new FacesTool ();

			Sidebar sidebar = Page.Sidebar as Sidebar;
			if (sidebar.IsActive (Page)) {
				faces_tool.Activate ();
				
				ShowFacesTool ();
			} else
				faces_tool.Deactivate ();
		}

		private void OnPhotoChanged (PhotoView sender)
		{
			if (!Page.InPhotoView)
				return;

			loaded_photo_ref.Target = sender.Item.Current;

			if (faces_tool != null) {
				faces_tool.Dispose ();
				faces_tool = null;
			}

			sender.View.PhotoLoaded += OnPhotoLoaded;
		}

		private void OnPhotoLoaded (object sender, EventArgs e)
		{
			((PhotoImageView) sender).PhotoLoaded -= OnPhotoLoaded;

			faces_tool = new FacesTool ();

			Sidebar sidebar = Page.Sidebar as Sidebar;
			if (sidebar.IsActive (Page)) {
				faces_tool.Activate ();
				
				ShowFacesTool ();
			} else
				faces_tool.Deactivate ();
		}
	}

	public class FaceSelectionWidget : SaneTreeView {
		
		Db database;
		internal FaceStore FaceStore;
		
		// FIXME this is a hack.
		private static Pixbuf empty_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, 1, 1);
		
		// If these are changed, the base () call in the constructor must be updated.
		private const int IdColumn = 0;
		private const int NameColumn = 1;

		public Face FaceAtPosition (double x, double y)
		{
			return FaceAtPosition((int) x, (int) y);
		}
		
		public Face FaceAtPosition (int x, int y)
		{
			TreePath path;
			
			// Work out which face we're dropping onto
			if (!this.GetPathAtPos (x, y, out path))
				return null;
			
			return FaceByPath (path);
		}
		
		public Face FaceByPath (TreePath path)
		{
			TreeIter iter;
			
			if (!Model.GetIter (out iter, path))
				return null;
			
			return FaceByIter (iter);
		}
		
		public Face FaceByIter (TreeIter iter)
		{
			GLib.Value val = new GLib.Value ();
			
			Model.GetValue (iter, IdColumn, ref val);
			uint face_id = (uint) val;
			
			return FaceStore.Get (face_id);
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
					faces[i] = FaceStore.Get (face_id);
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

			Face [] faces = FaceStore.GetAll ();
			Array.Sort (faces);
			foreach (Face face in faces) {
				store.AppendValues (face.Id, face.Name);
			}
		}
		
		// TODO Each face should have an icon.
		private void IconDataFunc (TreeViewColumn column,
		                           CellRenderer renderer,
		                           TreeModel model,
		                           TreeIter iter)
		{
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
			
			Face face = FaceStore.Get (face_id);
			if (face == null)
				return;
			
			(renderer as CellRendererText).Text = face.Name;
		}
		
		private bool TreeIterForFace (Face face, out TreeIter iter)
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
		
		// Insert face into the correct place in the tree.
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
				compare = FaceStore.Get ((uint) value);
				
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
			Face face = FaceStore.Get (face_id);
			
			// Ignore if it hasn't changed
			if (face.Name == args.NewText)
				return;
			
			face.Name = args.NewText;
			FaceStore.Commit (face);
			
			text_render.Edited -= HandleFaceNameEdited;
			
			args.RetVal = true;
		}

		public void HandleFaceSelectionButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button != 3)
				return;

			FacePopup popup = new FacePopup ();
			popup.Activate (args.Event, FaceAtPosition (args.Event.X, args.Event.Y), FaceHighlight);
			args.RetVal = true;
		}

		// This ConnectBefore is needed because otherwise the editability of the name
		// column will steal returns, spaces, and clicks if the face name is focused
		[GLib.ConnectBefore]
		public void HandleFaceSelectionKeyPress (object sender, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = true;
			
			switch (args.Event.Key) {
			case Gdk.Key.Delete:
				HandleDeleteSelectedFaceCommand (sender, (EventArgs) args);
				break;
				
			case Gdk.Key.F2:
				EditSelectedFaceName ();
				break;
				
			default:
				args.RetVal = false;
				break;
			}
		}

		public void HandleDeleteSelectedFaceCommand (object sender, EventArgs args)
		{
			Face [] faces = FaceHighlight;

			FaceLocationStore face_location_store = App.Instance.Database.FaceLocations;
			ISet<uint> photo_ids = new HashSet<uint> ();
			foreach (Face face in faces) {
				Dictionary<uint, FaceLocation> face_locations =
					face_location_store.GetFaceLocationsByFace (face);

				foreach (uint photo_id in face_locations.Keys)
					photo_ids.Add (photo_id);
			}

			int associated_photos = photo_ids.Count;

			string header;
			if (faces.Length == 1)
				header = String.Format (Catalog.GetString ("Delete face \"{0}\"?"), faces [0].Name.Replace ("_", "__"));
			else
				header = String.Format (Catalog.GetString ("Delete the {0} selected faces?"), faces.Length);
			
			header = String.Format (header, faces.Length);
			string msg = String.Empty;
			if (associated_photos > 0) {
				string photodesc = Catalog.GetPluralString ("photo", "photos", associated_photos);
				msg = String.Format(
					Catalog.GetPluralString("If you delete this face, the association with {0} {1} will be lost.",
				                        "If you delete these faces, the association with {0} {1} will be lost.",
				                        faces.Length),
					associated_photos, photodesc);
			}
			string ok_caption = Catalog.GetPluralString ("_Delete face", "_Delete faces", faces.Length);
			
			if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(App.Instance.Organizer.Window,
			                                                           DialogFlags.DestroyWithParent,
			                                                           MessageType.Warning,
			                                                           header,
			                                                           msg,
			                                                           ok_caption)) {

				App.Instance.Database.Faces.Remove (faces);
			}
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
			
			FaceStore = face_store;
			
			Update ();
			
			FaceStore.ItemsAdded += HandleFacesAdded;
			FaceStore.ItemsRemoved += HandleFacesRemoved;
			FaceStore.ItemsChanged += HandleFacesChanged;

			ButtonPressEvent += HandleFaceSelectionButtonPressEvent;
			KeyPressEvent += HandleFaceSelectionKeyPress;

			EnableSearch = true;
			SearchColumn = NameColumn;
			
			// Transparent white
			empty_pixbuf.Fill (0xffffff00);
		}
	}
}
