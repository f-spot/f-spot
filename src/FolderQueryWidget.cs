/*
 * FSpot.Gui.FolderQueryWidget.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Text;
using System.Collections;

using Gtk;

using FSpot;
using FSpot.Utils;
using FSpot.Query;

namespace FSpot.Gui
{
	
	
	public class FolderQueryWidget : HBox
	{
		PhotoQuery query;
		FolderSet folder_set;
		
		public FolderQueryWidget ()
		{
			folder_set = new FolderSet ();
			query = MainWindow.Toplevel.Query;
			
			query.SetCondition (folder_set);
			
			Drag.DestSet (this, DestDefaults.All,
			              folder_query_widget_source_table,
			              Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}
		
		private void UpdateGui ()
		{
			while (Children.Length != 0)
				Remove (Children[0]);
			
			int length = folder_set.UriList.Count;
			
			if (length == 0) {
				Hide ();
				return;
			}
			
			if (length < 4) {
				
				foreach (Uri uri in folder_set.UriList) {
					Image image = new Image ("gtk-directory", IconSize.Button);
					image.TooltipText = uri.ToString ();
					PackStart (image);
				}
				
				TooltipText = String.Empty;
				
			} else {
				
				Label label = new Label (String.Format ("<i>{0}x</i>", length));
				label.UseMarkup = true;
				PackStart (label);
				
				Image image = new Image ("gtk-directory", IconSize.Button);
				PackStart (image);
				
				StringBuilder builder = new StringBuilder ();
				foreach (Uri uri in folder_set.UriList) {
					if (builder.Length > 0)
						builder.AppendLine ();
					
					builder.Append (uri.ToString ());
				}
				
				TooltipText = builder.ToString ();
			}
			
			ShowAll ();
		}
		
		public void SetFolders (UriList uri_list)
		{
			folder_set.UriList = uri_list;
			
			UpdateGui ();
		}
		
		public void Clear ()
		{
			folder_set.UriList = null;
		}
		
		private static TargetEntry [] folder_query_widget_source_table =
			new TargetEntry [] {
				DragDropTargets.UriQueryEntry
		};
		
		protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y, Gtk.SelectionData selection_data, uint info, uint time_)
		{
			base.OnDragDataReceived (context, x, y, selection_data, info, time_);
			
			SetFolders (selection_data.GetUriListData ());
			query.RequestReload ();
		}

	}
}
