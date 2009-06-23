/*
 * FSpot.Widgets.FolderTreePage.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Collections.Generic;

using Gtk;

using GLib;

using FSpot;
using FSpot.Gui;
using FSpot.Utils;

using Banshee.Database;

using Mono.Unix;
using Mono.Data.SqliteClient;



namespace FSpot.Widgets
{
	
	
	public class FolderTreePage : SidebarPage
	{
		private readonly FolderTreeWidget folder_tree_widget;
		
		
		
		public FolderTreePage () 
			: base (new ScrolledWindow (), Catalog.GetString ("Folders"), "gtk-directory")
		{
			ScrolledWindow scrolled_window = SidebarWidget as ScrolledWindow;
			folder_tree_widget = new FolderTreeWidget ();
			scrolled_window.Add (folder_tree_widget);
		}
		
		protected override void AddedToSidebar () {
		}
	}
	
	public class FolderTreeWidget : SaneTreeView
	{	
		FolderTreeModel folder_tree_model;
	
		public FolderTreeWidget ()
		{		
			folder_tree_model = new FolderTreeModel ();
			Model = folder_tree_model;
			
			HeadersVisible = false;
			
			TreeViewColumn column = new TreeViewColumn ();
			
			CellRendererPixbuf pixbuf_renderer = new CellRendererPixbuf ();
			column.PackStart (pixbuf_renderer, false);
			column.SetCellDataFunc (pixbuf_renderer, PixbufDataFunc);
			
			CellRendererTextProgress folder_renderer = new CellRendererTextProgress ();
			column.PackStart (folder_renderer, true);
			column.SetCellDataFunc (folder_renderer, FolderDataFunc);
			
			AppendColumn (column);
			
			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    folder_tree_source_target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}
		
		public UriList SelectedUris {
			get {
				UriList list = new UriList ();
				
				TreePath[] selected_rows = Selection.GetSelectedRows ();
				
				foreach (TreePath row in selected_rows)
					list.Add (folder_tree_model.GetUriByPath (row));
				
				return list;
			}
		}
		
		private void PixbufDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererPixbuf renderer = cell as CellRendererPixbuf;
			string text = folder_tree_model.GetFolderNameByIter (iter);
			
			string stock;
			File file = FileFactory.NewForUri (folder_tree_model.GetUriByIter (iter));
			try {
				FileInfo info =
					file.QueryInfo ("standard::icon", FileQueryInfoFlags.None, null);
				
				ThemedIcon themed_icon = info.Icon as ThemedIcon;
				if (themed_icon != null && themed_icon.Names.Length > 0)
					stock = themed_icon.Names[0];
				else
					stock = "gtk-directory";
				
			} catch (Exception e) {
				stock = "gtk-directory";
			}

			TreeIter tmp;
			if (tree_model.IterParent (out tmp, iter)) {
				renderer.IconName = stock;
				renderer.CellBackground = null;
			} else {
				renderer.IconName = stock;
				renderer.CellBackgroundGdk = Style.Background (StateType.Selected);
			}
		}
		
		private void FolderDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererTextProgress renderer = cell as CellRendererTextProgress;
			
			int progress_value = 0;
			int count = (tree_model as FolderTreeModel).Count;
			
			if (count != 0)
				progress_value = (int) ((100.0 * folder_tree_model.GetPhotoCountByIter (iter)) / count);
			
			renderer.Value = progress_value;
			
			string text = folder_tree_model.GetFolderNameByIter (iter);
			
			TreeIter tmp;
			if (tree_model.IterParent (out tmp, iter)) {
				renderer.UseMarkup = false;
				renderer.Text = text;
				renderer.CellBackground = null;
			} else {
				renderer.UseMarkup = true;
				
				/* since import do not use GIO at the moment, no other prefix than file:/// is
				 * possible.
				 */
				if (text == Uri.UriSchemeFile)
					renderer.Text = String.Format ("<b>{0}</b>", Catalog.GetString ("Filesystem"));
				else
					renderer.Text = String.Format ("<b>{0}</b>", text);
				
				renderer.CellBackgroundGdk = Style.Background (StateType.Selected);
			}
		}
			
		private string GetStock (string scheme)
		{
			/* not very usefull at the moment */
			if (scheme == Uri.UriSchemeFile)
				return "gtk-directory";
		
			return "gtk-directory";
		}
		
		private static TargetEntry [] folder_tree_source_target_table =
			new TargetEntry [] {
				DragDropTargets.UriQueryEntry,
				DragDropTargets.UriListEntry,
				DragDropTargets.PlainTextEntry
		};
		
		
		protected override void OnDragDataGet (Gdk.DragContext context, Gtk.SelectionData selection_data, uint info, uint time_)
		{
			if (info == DragDropTargets.UriQueryEntry.Info
			    || info == DragDropTargets.UriListEntry.Info
			    || info == DragDropTargets.PlainTextEntry.Info) {
				
				selection_data.SetUriListData (SelectedUris, context.Targets[0]);
				return;
			}
		}
		
		protected override bool OnDragDrop (Gdk.DragContext context, int x, int y, uint time_)
		{
			return true;
		}
		
		protected override void OnRowActivated (Gtk.TreePath path, Gtk.TreeViewColumn column)
		{
			MainWindow.Toplevel.SetFolderQuery (SelectedUris);	
		}

	}

	
	/*
	 * Because subclassing of CellRendererText does not to work, we
	 * use a new cellrenderer, which renderes a simple text and a
	 * progress bar below the text similar to the one used in baobab (gnome-utils)
	 */
	public class CellRendererTextProgress : CellRenderer
	{
		readonly int progress_width;
		readonly int progress_height;
		
		Gdk.Color green;
		Gdk.Color yellow;
		Gdk.Color red;
		
		public CellRendererTextProgress (int progress_width, int progress_height)
		{
			this.progress_width = progress_width;
			this.progress_height = progress_height;
			
			Xalign = 0.0f;
			Yalign = 0.5f;
			
			Xpad = 2;
			Ypad = 2;
			
			green = new Gdk.Color (0xcc, 0x00, 0x00);
			yellow = new Gdk.Color (0xed, 0xd4, 0x00);
			red = new Gdk.Color (0x73, 0xd2, 0x16);
		}
		
		public CellRendererTextProgress () : this (70, 8)
		{
		}
		
		int progress_value;
		
		[GLib.PropertyAttribute ("value")]
		public int Value {
			get { return progress_value; }
			set {
				/* normalize value */
				progress_value = Math.Max (Math.Min (value, 100), 0);
			}
		}
		
		Pango.Layout text_layout;
		string text;
		
		[GLib.PropertyAttribute ("text")]
		public string Text {
			get { return text; }
			set {
				if (text == value)
					return;
				
				text = value;
				text_layout = null;	
			}
		}
		
		bool use_markup;
		public bool UseMarkup {
			get { return use_markup; }
			set {
				if (use_markup == value)
					return;
				
				use_markup = value;
				text_layout = null;
			}
		}
		
		private void UpdateLayout (Widget widget)
		{
			text_layout = new Pango.Layout (widget.PangoContext);

			if (UseMarkup)
				text_layout.SetMarkup (text);
			else
				text_layout.SetText (text);
		}
		
		private Gdk.Color GetValueColor ()
		{
			if (progress_value <= 33)
				return green;
			
			if (progress_value <= 66)
				return yellow;
			
			return red;
		}
		
		public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			if (text_layout == null)
				UpdateLayout (widget);
			
			int text_width, text_height;
			
			text_layout.GetPixelSize (out text_width, out text_height);
			
			width = (int) (2 * Xpad + Math.Max (progress_width, text_width));
			height = (int) (3 * Ypad + progress_height + text_height);
			
			x_offset = Math.Max ((int) (Xalign * (cell_area.Width - width)), 0);
			y_offset = Math.Max ((int) (Yalign * (cell_area.Height - height)), 0);
		}
		
		protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
		{
			base.Render (window, widget, background_area, cell_area, expose_area, flags);
			
			if (text_layout == null)
				UpdateLayout (widget);

			int x, y, width, height, text_width, text_height;
			
			/* first render the text */
			text_layout.GetPixelSize (out text_width, out text_height);
			
			x  = (int) (cell_area.X + Xpad + Math.Max ((int) (Xalign * (cell_area.Width - 2 * Xpad - text_width)), 0));
			y  = (int) (cell_area.Y + Ypad);

			Style.PaintLayout (widget.Style,
			                   window,
			                   StateType.Normal,
			                   true,
			                   cell_area,
			                   widget,
			                   "cellrenderertextprogress",
			                   x, y,
			                   text_layout);
			
			y += (int) (text_height + Ypad);
			x  = (int) (cell_area.X + Xpad + Math.Max ((int) (Xalign * (cell_area.Width - 2 * Xpad - progress_width)), 0));
			
			
			/* second render the progress bar */
			
			/* dispose cairo object after usage */
			using (Cairo.Context cairo_context = Gdk.CairoHelper.Create (window)) {
				
				width = progress_width;
				height = progress_height;
				
				cairo_context.Rectangle (x, y, width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, widget.Style.Dark (StateType.Normal));
				cairo_context.Fill ();
				
				x += widget.Style.XThickness;
				y += widget.Style.XThickness;
				width -= 2* widget.Style.XThickness;
				height -= 2 * widget.Style.Ythickness;
				
				cairo_context.Rectangle (x, y, width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, widget.Style.Light (StateType.Normal));
				cairo_context.Fill ();
				
				/* scale the value and ensure, that at least one pixel is drawn, if the value is greater than zero */
				int scaled_width =
					(int) Math.Max (((progress_value * width) / 100.0),
					                (progress_value == 0)? 0 : 1);
				
				cairo_context.Rectangle (x, y, scaled_width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, GetValueColor ());
				cairo_context.Fill ();
			}
		}

	}
	
	
	public class FolderTreeModel : TreeStore
	{
		Db database;
		
		const string query_string = 
			"SELECT base_uri, COUNT(*) AS count " +
			"FROM photos " + 
			"GROUP BY base_uri " +
			"ORDER BY base_uri DESC";
		
		
		public FolderTreeModel ()
			: base (typeof (string), typeof (int), typeof (Uri))
		{
			database = MainWindow.Toplevel.Database;
			database.Photos.ItemsChanged += HandlePhotoItemsChanged;
			
			UpdateFolderTree ();
		}
		
		void HandlePhotoItemsChanged (object sender, DbItemEventArgs<Photo> e)
		{
			UpdateFolderTree ();
		}
		
		public string GetFolderNameByIter (TreeIter iter)
		{
			if ( ! IterIsValid (iter))
				return null;
			
			return (string) GetValue (iter, 0);
		}
		
		public int GetPhotoCountByIter (TreeIter iter)
		{
			if ( ! IterIsValid (iter))
				return -1;
			
			return (int) GetValue (iter, 1);
		}
		
		public Uri GetUriByIter (TreeIter iter)
		{
			if ( ! IterIsValid (iter))
				return null;
			
			return (Uri) GetValue (iter, 2);
		}	
		
		public Uri GetUriByPath (TreePath row)
		{
			TreeIter iter;
			
			GetIter (out iter, row);
			
			return GetUriByIter (iter);
		}
		
		int count_all;
		public int Count {
			get { return count_all; }
		}
		
		/*
		 * UpdateFolderTree queries for directories in database and updates
		 * a possibly existing folder-tree to the queried structure
		 */
		private void UpdateFolderTree ()
		{
			Clear ();
			
			count_all = 0;
			
			/* points at start of each iteration to the leaf of the last inserted uri */
			TreeIter iter = TreeIter.Zero;
			
			/* stores the segments of the last inserted uri */
			string[] last_segments = new string[] {};
			
			int last_count = 0;
			
			SqliteDataReader reader = database.Database.Query (query_string);
			
			while (reader.Read ()) {
				Uri base_uri = new Uri (reader["base_uri"].ToString ());
				
				if ( ! base_uri.IsAbsoluteUri) {
					FSpot.Utils.Log.Error ("Uri must be absolute: {0}", base_uri.ToString ());
					continue;
				}
				
				int count = Convert.ToInt32 (reader["count"]);
				
				string[] segments = base_uri.Segments;

				/* 
				 * since we have an absolute uri, first segement starts with "/" according
				 * to the msdn doc. So we can overwrite the first segment for our needs and
				 * put the scheme here.
				 */
				segments[0] = base_uri.Scheme;
				
				int i = 0;
				
				/* find first difference of last inserted an current uri */
				while (i < last_segments.Length && i < segments.Length) {
					
					/* remove suffix '/', which are appended to every directory (see msdn-doc) */
					segments[i] = segments[i].TrimEnd ('/');
					
					if (segments[i] != last_segments[i])
						break;
					
					i++;
				}
				
				/* points to the parent node of the current iter */
				TreeIter parent_iter = iter;
				
				/* step back to the level, where the difference occur */
				for (int j = 0; j + i < last_segments.Length; j++) {
					
					iter = parent_iter;
					
					if (IterParent (out parent_iter, iter)) { 
						last_count += (int)GetValue (parent_iter, 1);
						SetValue (parent_iter, 1, last_count);
					} else
						count_all += (int)last_count;
				}
				
				while (i < segments.Length) {
					segments[i] = segments[i].TrimEnd ('/');
					
					if (IterIsValid (parent_iter))
						iter =
							AppendValues (parent_iter,
							              segments[i],
							              (segments.Length - 1 == i)? count : 0,
							              new Uri ((Uri) GetValue (parent_iter, 2),
							                       String.Format ("{0}/", segments[i]))
							              );
					else
						iter =
							AppendValues (segments[i],
							              (segments.Length - 1 == i)? count : 0,
							              new Uri (base_uri, "/"));
					
					parent_iter = iter;
					
					i++;
				}
				
				last_count = count;
				last_segments = segments;
				
			}
			
			/* and at least, step back and update photo count */
			while (IterParent (out iter, iter)) {
				last_count += (int)GetValue (iter, 1);
				SetValue (iter, 1, last_count);
			}
			count_all += (int)last_count;
		}
	}
}
