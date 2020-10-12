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

using FSpot.Utils;

using Gtk;

using Hyena;

using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreeView : SaneTreeView
	{
		readonly FolderTreeModel folderTreeModel;

		protected FolderTreeView (IntPtr raw) : base (raw) { }

		static readonly TargetList folderTreeSourceTargetList = new TargetList ();

		static FolderTreeView ()
		{
			folderTreeSourceTargetList.AddTextTargets ((uint)DragDropTargets.TargetType.PlainText);
			folderTreeSourceTargetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
			folderTreeSourceTargetList.AddTargetEntry (DragDropTargets.UriQueryEntry);
		}

		public FolderTreeView () : this (new FolderTreeModel ())
		{
		}

		public FolderTreeView (FolderTreeModel treeModel) : base (treeModel)
		{
			folderTreeModel = treeModel;

			HeadersVisible = false;

			using var column = new TreeViewColumn ();

			using var pixbufRenderer = new CellRendererPixbuf ();
			column.PackStart (pixbufRenderer, false);
			column.SetCellDataFunc (pixbufRenderer, PixbufDataFunc);

			using var folderRenderer = new CellRendererTextProgress ();
			column.PackStart (folderRenderer, true);
			column.SetCellDataFunc (folderRenderer, FolderDataFunc);

			AppendColumn (column);

			Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
					(TargetEntry[])folderTreeSourceTargetList, Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}

		public UriList SelectedUris {
			get {
				var list = new UriList ();

				var selectedRows = Selection.GetSelectedRows ();

				foreach (TreePath row in selectedRows)
					list.Add (new SafeUri (folderTreeModel.GetUriByPath (row)));

				return list;
			}
		}

		void PixbufDataFunc (TreeViewColumn treeColumn, CellRenderer cell, TreeModel treeModel, TreeIter iter)
		{
			var uri = folderTreeModel.GetUriByIter (iter);
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

		void FolderDataFunc (TreeViewColumn treeColumn, CellRenderer cell, TreeModel treeModel, TreeIter iter)
		{
			var renderer = cell as CellRendererTextProgress;

			int progress_value = 0;
			int count = (treeModel as FolderTreeModel).Count;

			if (count != 0)
				progress_value = (int)((100.0 * folderTreeModel.GetPhotoCountByIter (iter)) / count);

			renderer.Value = progress_value;

			string text = folderTreeModel.GetFolderNameByIter (iter);
			if (treeModel.IterParent (out _, iter)) {
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

		protected override void OnDragDataGet (Gdk.DragContext context, Gtk.SelectionData selectionData, uint info, uint time)
		{
			if (info == DragDropTargets.UriQueryEntry.Info
				|| info == (uint)DragDropTargets.TargetType.UriList
				|| info == (uint)DragDropTargets.TargetType.PlainText) {

				selectionData.SetUriListData (SelectedUris, context.Targets[0]);
				return;
			}
		}

		protected override bool OnDragDrop (Gdk.DragContext context, int x, int y, uint time)
		{
			return true;
		}

		protected override void OnRowActivated (Gtk.TreePath path, Gtk.TreeViewColumn column)
		{
			App.Instance.Organizer.SetFolderQuery (SelectedUris);
		}
	}
}
