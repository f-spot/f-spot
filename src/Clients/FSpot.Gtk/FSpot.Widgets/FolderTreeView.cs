//
// FolderTreeView.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using FSpot.Utils;

using Hyena;
using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreeView : SaneTreeView
	{
		FolderTreeModel folder_tree_model;

		protected FolderTreeView (IntPtr raw) : base (raw) { }

		static TargetList folderTreeSourceTargetList = new TargetList ();

		static FolderTreeView ()
		{
			folderTreeSourceTargetList.AddTextTargets ((uint)DragDropTargets.TargetType.PlainText);
			folderTreeSourceTargetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
			folderTreeSourceTargetList.AddTargetEntry (DragDropTargets.UriQueryEntry);
		}

		public FolderTreeView () : this (new FolderTreeModel ())
		{
		}

		public FolderTreeView (FolderTreeModel tree_model) : base (tree_model)
		{
			folder_tree_model = tree_model;

			HeadersVisible = false;

			var column = new TreeViewColumn ();

			var pixbuf_renderer = new CellRendererPixbuf ();
			column.PackStart (pixbuf_renderer, false);
			column.SetCellDataFunc (pixbuf_renderer, PixbufDataFunc);

			var folder_renderer = new CellRendererTextProgress ();
			column.PackStart (folder_renderer, true);
			column.SetCellDataFunc (folder_renderer, FolderDataFunc);

			AppendColumn (column);

			Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
					(TargetEntry[])folderTreeSourceTargetList, Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}

		public UriList SelectedUris {
			get {
				var list = new UriList ();

				var selected_rows = Selection.GetSelectedRows ();

				foreach (TreePath row in selected_rows)
					list.Add (new SafeUri (folder_tree_model.GetUriByPath (row)));

				return list;
			}
		}

		void PixbufDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var uri = folder_tree_model.GetUriByIter (iter);
			if (uri == null)
				return;

			var renderer = cell as CellRendererPixbuf;

			string stock;

			// FIXME, icon theme support
			//File file = FileFactory.NewForUri (uri);
			//try {
			//	//FileInfo info = file.QueryInfo ("standard::icon", FileQueryInfoFlags.None, null);
			//	var icon = System.Drawing.Icon.ExtractAssociatedIcon (uri.AbsolutePath);
			//	if (icon != null && icon.Names.Length > 0)
			//		stock = icon;
			//	else
			//		stock = "gtk-directory";

			//} catch (Exception) {
			//	stock = "gtk-directory";
			//}

			//if (tree_model.IterParent (out var tmp, iter)) {
			//	renderer.IconName = stock;
			//	renderer.CellBackground = null;
			//} else {
			//	renderer.IconName = stock;
			//	renderer.CellBackgroundGdk = Style.Background (StateType.Selected);
			//}
		}

		void FolderDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var renderer = cell as CellRendererTextProgress;

			int progress_value = 0;
			int count = (tree_model as FolderTreeModel).Count;

			if (count != 0)
				progress_value = (int)((100.0 * folder_tree_model.GetPhotoCountByIter (iter)) / count);

			renderer.Value = progress_value;

			string text = folder_tree_model.GetFolderNameByIter (iter);

			if (tree_model.IterParent (out var tmp, iter)) {
				renderer.UseMarkup = false;
				renderer.Text = text;
				renderer.CellBackground = null;
			} else {
				renderer.UseMarkup = true;

				/* since import do not use GIO at the moment, no other prefix than file:/// is
				 * possible.
				 */
				if (text == Uri.UriSchemeFile)
					renderer.Text = $"<b>{Catalog.GetString ("Filesystem")}</b>";
				else
					renderer.Text = $"<b>{text}</b>";

				renderer.CellBackgroundGdk = Style.Background (StateType.Selected);
			}
		}

		protected override void OnDragDataGet (Gdk.DragContext context, Gtk.SelectionData selection_data, uint info, uint time_)
		{
			if (info == DragDropTargets.UriQueryEntry.Info
				|| info == (uint)DragDropTargets.TargetType.UriList
				|| info == (uint)DragDropTargets.TargetType.PlainText) {

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
