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

using Gtk;
using GLib;

using FSpot.Utils;

using Hyena;
using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreeView : SaneTreeView
	{
		FolderTreeModel folder_tree_model;

		protected FolderTreeView (IntPtr raw) : base (raw) {}

        static TargetList folderTreeSourceTargetList = new TargetList();

        static FolderTreeView()
        {
            folderTreeSourceTargetList.AddTextTargets((uint)DragDropTargets.TargetType.PlainText);
            folderTreeSourceTargetList.AddUriTargets((uint)DragDropTargets.TargetType.UriList);
            folderTreeSourceTargetList.AddTargetEntry(DragDropTargets.UriQueryEntry);
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
			var renderer = cell as CellRendererPixbuf;

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
					renderer.Text = string.Format ("<b>{0}</b>", Catalog.GetString ("Filesystem"));
				else
					renderer.Text = string.Format ("<b>{0}</b>", text);

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
