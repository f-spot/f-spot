/*
 * FSpot.Widgets.FolderTreeView.cs
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
using FSpot.Utils;

using Hyena;
using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreeView : SaneTreeView
	{	
		FolderTreeModel folder_tree_model;
	
		public FolderTreeView () : this (new FolderTreeModel ())
		{
		}

		public FolderTreeView (FolderTreeModel tree_model) : base (tree_model)
		{		
			folder_tree_model = tree_model;
			
			HeadersVisible = false;
			
			TreeViewColumn column = new TreeViewColumn ();
			
			CellRendererPixbuf pixbuf_renderer = new CellRendererPixbuf ();
			column.PackStart (pixbuf_renderer, false);
			column.SetCellDataFunc (pixbuf_renderer, PixbufDataFunc as TreeCellDataFunc);
			
			CellRendererTextProgress folder_renderer = new CellRendererTextProgress ();
			column.PackStart (folder_renderer, true);
			column.SetCellDataFunc (folder_renderer, FolderDataFunc as TreeCellDataFunc);
			
			AppendColumn (column);
			
			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    folder_tree_source_target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}
		
		public UriList SelectedUris {
			get {
				UriList list = new UriList ();
				
				TreePath[] selected_rows = Selection.GetSelectedRows ();
				
				foreach (TreePath row in selected_rows)
					list.Add (new SafeUri (folder_tree_model.GetUriByPath (row)));
				
				return list;
			}
		}
		
		void PixbufDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererPixbuf renderer = cell as CellRendererPixbuf;
			
			string stock;
			var uri = folder_tree_model.GetUriByIter (iter);
			if (uri == null)
				return;
			File file = FileFactory.NewForUri (uri);
			try {
				FileInfo info =
					file.QueryInfo ("standard::icon", FileQueryInfoFlags.None, null);
				
				ThemedIcon themed_icon = info.Icon as ThemedIcon;
				if (themed_icon != null && themed_icon.Names.Length > 0)
					stock = themed_icon.Names[0];
				else
					stock = "gtk-directory";
				
			} catch (Exception) {
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
		
		void FolderDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
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
			App.Instance.Organizer.SetFolderQuery (SelectedUris);	
		}

	}
}
