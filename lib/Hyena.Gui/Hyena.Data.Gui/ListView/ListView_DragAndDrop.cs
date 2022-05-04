//
// ListView_DragAndDrop.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace Hyena.Data.Gui
{
	public static class ListViewDragDropTarget
	{
		public enum TargetType
		{
			ModelSelection
		}

		public static readonly TargetEntry ModelSelection =
			new TargetEntry ("application/x-hyena-data-model-selection", TargetFlags.App,
				(uint)TargetType.ModelSelection);
	}

	public partial class ListView<T> : ListViewBase
	{
		static TargetEntry[] drag_drop_dest_entries = new TargetEntry[] {
			ListViewDragDropTarget.ModelSelection
		};

		protected virtual TargetEntry[] DragDropDestEntries {
			get { return drag_drop_dest_entries; }
		}

		protected virtual TargetEntry[] DragDropSourceEntries {
			get { return drag_drop_dest_entries; }
		}

		bool is_reorderable = false;
		public bool IsReorderable {
			get { return is_reorderable && IsEverReorderable; }
			set {
				is_reorderable = value;
				OnDragSourceSet ();
				OnDragDestSet ();
			}
		}

		bool is_ever_reorderable = false;
		public bool IsEverReorderable {
			get { return is_ever_reorderable; }
			set {
				is_ever_reorderable = value;
				OnDragSourceSet ();
				OnDragDestSet ();
			}
		}

		bool force_drag_source_set = false;
		protected bool ForceDragSourceSet {
			get { return force_drag_source_set; }
			set {
				force_drag_source_set = true;
				OnDragSourceSet ();
			}
		}

		bool force_drag_dest_set = false;
		protected bool ForceDragDestSet {
			get { return force_drag_dest_set; }
			set {
				force_drag_dest_set = true;
				OnDragDestSet ();
			}
		}

		protected virtual void OnDragDestSet ()
		{
			if (ForceDragDestSet || IsReorderable) {
				Gtk.Drag.DestSet (this, DestDefaults.All, DragDropDestEntries, Gdk.DragAction.Move);
			} else {
				Gtk.Drag.DestUnset (this);
			}
		}

		protected virtual void OnDragSourceSet ()
		{
			if (ForceDragSourceSet || IsReorderable) {
				Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
					DragDropSourceEntries, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			} else {
				Gtk.Drag.SourceUnset (this);
			}
		}

		uint drag_scroll_timeout_id;
		uint drag_scroll_timeout_duration = 50;
		double drag_scroll_velocity;
		double drag_scroll_velocity_max = 100.0;
		int drag_reorder_row_index = -1;
		int drag_reorder_motion_y = -1;

		void StopDragScroll ()
		{
			drag_scroll_velocity = 0.0;

			if (drag_scroll_timeout_id > 0) {
				GLib.Source.Remove (drag_scroll_timeout_id);
				drag_scroll_timeout_id = 0;
			}
		}

		void OnDragScroll (GLib.TimeoutHandler handler, double threshold, int total, int position)
		{
			if (position < threshold) {
				drag_scroll_velocity = -1.0 + (position / threshold);
			} else if (position > total - threshold) {
				drag_scroll_velocity = 1.0 - ((total - position) / threshold);
			} else {
				StopDragScroll ();
				return;
			}

			if (drag_scroll_timeout_id == 0) {
				drag_scroll_timeout_id = GLib.Timeout.Add (drag_scroll_timeout_duration, handler);
			}
		}

		protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
		{
			if (!IsReorderable) {
				StopDragScroll ();
				drag_reorder_row_index = -1;
				drag_reorder_motion_y = -1;
				InvalidateList ();
				return false;
			}

			drag_reorder_motion_y = y;
			DragReorderUpdateRow ();

			OnDragScroll (OnDragVScrollTimeout, Allocation.Height * 0.3, Allocation.Height, y);

			return true;
		}

		protected override void OnDragLeave (Gdk.DragContext context, uint time)
		{
			StopDragScroll ();
		}

		protected override void OnDragEnd (Gdk.DragContext context)
		{
			StopDragScroll ();
			drag_reorder_row_index = -1;
			drag_reorder_motion_y = -1;
			InvalidateList ();
		}

		bool OnDragVScrollTimeout ()
		{
			ScrollToY (VadjustmentValue + (drag_scroll_velocity * drag_scroll_velocity_max));
			DragReorderUpdateRow ();
			return true;
		}

		void DragReorderUpdateRow ()
		{
			int row = GetDragRow (drag_reorder_motion_y);
			if (row != drag_reorder_row_index) {
				drag_reorder_row_index = row;
				InvalidateList ();
			}
		}

		protected int GetDragRow (int y)
		{
			y = TranslateToListY (y);
			int row = GetModelRowAt (0, y);

			if (row == -1) {
				return -1;
			}

			if (row != GetModelRowAt (0, y + ChildSize.Height / 2)) {
				row++;
			}

			return row;
		}
	}
}
