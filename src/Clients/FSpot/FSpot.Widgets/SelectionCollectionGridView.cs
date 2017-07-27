//
// SelectionCollectionGridView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
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
using FSpot.Core;
using Gdk;
using Gtk;

namespace FSpot.Widgets
{
	// TODO: This event handler is a hack. The default event from a widget
	//       (DragBegin) should be used, but therfore, the event must be fired
	//       correctly.
	public delegate void StartDragHandler (object o, StartDragArgs args);

	public class SelectionCollectionGridView : CollectionGridView
	{
		public SelectionCollection Selection { get; private set; }

		// Focus Handling
		int real_focus_cell;

		public int FocusCell {
			get { return real_focus_cell; }
			set {
				if (value != real_focus_cell) {
					value = Math.Max (value, 0);
					value = Math.Min (value, Collection.Count - 1);
					InvalidateCell (value);
					InvalidateCell (real_focus_cell);
					real_focus_cell = value;
				}
			}
		}

		public SelectionCollectionGridView (IntPtr raw) : base (raw)
		{
		}

		public SelectionCollectionGridView (IBrowsableCollection collection) : base (collection)
		{
			Selection = new SelectionCollection (Collection);

			Selection.DetailedChanged += delegate(IBrowsableCollection sender, Int32[] ids) {
				if (ids == null)
					QueueDraw ();
				else
					foreach (int id in ids)
						InvalidateCell (id);
			};

			AddEvents ((int)EventMask.KeyPressMask
			| (int)EventMask.KeyReleaseMask
			| (int)EventMask.ButtonPressMask
			| (int)EventMask.ButtonReleaseMask
			| (int)EventMask.PointerMotionMask
			| (int)EventMask.PointerMotionHintMask);

			CanFocus = true;
		}

		#region Event Handlers

		public event EventHandler<BrowsableEventArgs> DoubleClicked;

		// TODO: hack. See definition of StartDragHandler
		public event StartDragHandler StartDrag;

		#endregion

		#region Drawing Methods

		protected override bool OnExposeEvent (EventExpose args)
		{
			bool ret = base.OnExposeEvent (args);

			foreach (Rectangle area in args.Region.GetRectangles ()) {
				DrawSelection (area);
			}

			return ret;
		}

		protected override void DrawCell (int cell_num, Rectangle cell_area, Rectangle expose_area)
		{
			DrawPhoto (cell_num, cell_area, expose_area, Selection.Contains (cell_num), Selection.Contains (cell_num) && FocusCell == cell_num);
		}

		void DrawSelection (Rectangle exposeArea)
		{
			if (!isRectSelection)
				return;

			Rectangle region;
			if (!exposeArea.Intersect (rect_select, out region))
				return;

			// draw selection
			using (Cairo.Context cairo_g = CairoHelper.Create (BinWindow)) {

				Color color = Style.Background (StateType.Selected);
				cairo_g.SetSourceColor (new Cairo.Color (color.Red / 65535.0, color.Green / 65535.0, color.Blue / 65535.0, 0.5));
				cairo_g.Rectangle (region.X, region.Y, region.Width, region.Height);
				cairo_g.Fill ();

			}
		}

		#endregion

		#region Utility Methods

		// TODO: move this to SelectionCollection
		public void SelectAllCells ()
		{
			Selection.Add (0, Collection.Count - 1);
		}

		protected virtual void ContextMenu (EventButton evnt, int cellNum)
		{
		}

		#endregion

		#region Event Handler

		// TODO: the following code need to be cleaned up.
		// TODO: rubberband selection behaves different than Gtk.IconView. This needs to be fixed.
		// TODO: selection by clicks behaves different than Gtk.IconView. This needs to be fixed.

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			int cell_num = CellAtPosition ((int)evnt.X, (int)evnt.Y);

			start_select_event = evnt;

			selection_start = new Point ((int)evnt.X, (int)evnt.Y);
			selection_modifier = evnt.State;

			isRectSelection = false;
			isDragDrop = false;

			switch (evnt.Type) {
			case EventType.TwoButtonPress:
				if (evnt.Button != 1 ||
				    (evnt.State & (ModifierType.ControlMask | ModifierType.ShiftMask)) != 0)
					return false;

				DoubleClicked?.Invoke (this, new BrowsableEventArgs (cell_num, null));
				return true;

			case EventType.ButtonPress:
				GrabFocus ();
				// on a cell : context menu if button 3
				// cell selection is done on button release
				if (evnt.Button == 3) {
					ContextMenu (evnt, cell_num);
					return true;
				}
				return false;
			}

			return false;
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (isRectSelection) {
				// remove scrolling and rectangular selection
				if (scroll_timeout != 0) {
					GLib.Source.Remove (scroll_timeout);
					scroll_timeout = 0;
				}

				isRectSelection = false;
				if (BinWindow != null) {
					BinWindow.InvalidateRect (rect_select, false);
					BinWindow.ProcessUpdates (true);
				}
				rect_select = new Rectangle ();
			} else if (!isDragDrop) {
				int cell_num = CellAtPosition ((int)evnt.X, (int)evnt.Y);
				if (cell_num != -1) {
					if ((evnt.State & ModifierType.ControlMask) != 0) {
						Selection.ToggleCell (cell_num);
					} else if ((evnt.State & ModifierType.ShiftMask) != 0) {
						Selection.Add (FocusCell, cell_num);
					} else {
						Selection.Clear ();
						Selection.Add (cell_num);
					}
					FocusCell = cell_num;
				}
			}
			isDragDrop = false;

			return true;
		}

		// rectangle of dragging selection
		Rectangle rect_select;
		Point selection_start;
		Point selection_end;
		ModifierType selection_modifier;

		bool isRectSelection;
		bool isDragDrop;

		// initial selection
		int[] start_select_selection;
		// initial event used to detect drag&drop
		EventButton start_select_event;
		// timer using when scrolling selection
		uint scroll_timeout;

		static Rectangle BoundedRectangle (Point p1, Point p2)
		{
			return new Rectangle (Math.Min (p1.X, p2.X),
				Math.Min (p1.Y, p2.Y),
				Math.Abs (p1.X - p2.X) + 1,
				Math.Abs (p1.Y - p2.Y) + 1);
		}

		protected Point GetPointer ()
		{
			int x, y;
			GetPointer (out x, out y);

			return new Point (x + (int)Hadjustment.Value, y + (int)Vadjustment.Value);
		}

		// during pointer motion, select/toggle pictures between initial x/y (param)
		// and current x/y (get pointer)
		void UpdateRubberband ()
		{
			// determine old and new selection
			var old_selection = rect_select;
			selection_end = GetPointer ();
			var new_selection = BoundedRectangle (selection_start, selection_end);

			// determine region to invalidate
			var region = Region.Rectangle (old_selection);
			region.Xor (Region.Rectangle (new_selection));
			region.Shrink (-1, -1);

			BinWindow.InvalidateRegion (region, true);

			rect_select = new_selection;
			UpdateRubberbandSelection ();
		}

		void UpdateRubberbandSelection ()
		{
			var selected_area = BoundedRectangle (selection_start, selection_end);

			// Restore initial selection
			var initial_selection = Selection.ToBitArray ();
			Selection.Clear (false);
			foreach (int i in start_select_selection)
				Selection.Add (i, false);

			// Set selection
			int first = -1;
			foreach (var cell_num in CellsInRect (selected_area)) {
				if (first == -1)
					first = cell_num;

				if ((selection_modifier & ModifierType.ControlMask) == 0)
					Selection.Add (cell_num, false);
				else
					Selection.ToggleCell (cell_num, false);
			}
			if (first != -1)
				FocusCell = first;

			// fire events for cells which have changed selection flag
			var new_selection = Selection.ToBitArray ();
			var selection_changed = initial_selection.Xor (new_selection);
			var changed = new List<int> ();
			for (int i = 0; i < selection_changed.Length; i++)
				if (selection_changed.Get (i))
					changed.Add (i);
			if (changed.Count != 0)
				Selection.SignalChange (changed.ToArray ());
		}

		// if scroll is required, a timeout is fired
		// until the button is release or the pointer is
		// in window again
		int deltaVscroll;

		bool HandleMotionTimeout ()
		{
			int new_x, new_y;

			// do scroll
			double newVadj = Vadjustment.Value;
			if (deltaVscroll < 130)
				deltaVscroll += 15;

			ModifierType new_mod;
			Display.GetPointer (out new_x, out new_y, out new_mod);
			GetPointer (out new_x, out new_y);

			if (new_y <= 0) {
				newVadj -= deltaVscroll;
				if (newVadj < 0)
					newVadj = 0;
			} else if ((new_y > Allocation.Height) &&
			           (newVadj < Vadjustment.Upper - Allocation.Height - deltaVscroll))
				newVadj += deltaVscroll;
			Vadjustment.Value = newVadj;

			UpdateRubberband ();// (new Point (new_x + (int) Hadjustment.Value, new_y + (int) Vadjustment.Value));

			Vadjustment.ChangeValue ();

			// stop firing timeout when no button pressed
			return (new_mod & (ModifierType.Button1Mask | ModifierType.Button3Mask)) != 0;
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if ((evnt.State & (ModifierType.Button1Mask | ModifierType.Button3Mask)) == 0)
				return false;

			if (!Gtk.Drag.CheckThreshold (this, selection_start.X, selection_start.Y,
			    (int)evnt.X, (int)evnt.Y))
				return false;

			if (isRectSelection) {
				// scroll if out of window
				double d_x, d_y;
				deltaVscroll = 30;

				if (EventHelper.GetCoords (evnt, out d_x, out d_y)) {
					int new_y = (int)d_y;
					if ((new_y <= 0) || (new_y >= Allocation.Height)) {
						if (scroll_timeout == 0)
							scroll_timeout = GLib.Timeout.Add (100, new GLib.TimeoutHandler (HandleMotionTimeout));
					} else if (scroll_timeout != 0) {
						GLib.Source.Remove (scroll_timeout);
						scroll_timeout = 0;
					}
				} else if (scroll_timeout != 0) {
					GLib.Source.Remove (scroll_timeout);
					scroll_timeout = 0;
				}

				// handle selection
				UpdateRubberband ();
				//SelectMotion (new Point ((int) args.Event.X, (int) args.Event.Y));
			} else {
				int cell_num = CellAtPosition (selection_start);

				if (Selection.Contains (cell_num)) {
					// on a selected cell : do drag&drop
					isDragDrop = true;
					if (StartDrag != null) {
						uint but = (evnt.State & ModifierType.Button1Mask) != 0 ? 1U : 3U;
						StartDrag (this, new StartDragArgs (but, start_select_event));
					}
				} else {
					// not on a selected cell : do rectangular select
					isRectSelection = true;

					// ctrl : toggle selected, shift : keep selected
					if ((evnt.State & (ModifierType.ShiftMask | ModifierType.ControlMask)) == 0)
						Selection.Clear ();

					start_select_selection = Selection.Ids; // keep initial selection
					// no rect draw at beginning
					rect_select = Rectangle.Zero;

					return false;
				}
			}

			return true;
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			int focus_old = FocusCell;

			bool shift = ModifierType.ShiftMask == (evnt.State & ModifierType.ShiftMask);
			bool control = ModifierType.ControlMask == (evnt.State & ModifierType.ControlMask);

			switch (evnt.Key) {
			case Gdk.Key.Down:
			case Gdk.Key.J:
			case Gdk.Key.j:
				FocusCell += VisibleColums;
				break;

			case Gdk.Key.Left:
			case Gdk.Key.H:
			case Gdk.Key.h:
				if (control && shift)
					FocusCell -= FocusCell % VisibleColums;
				else
					FocusCell--;
				break;

			case Gdk.Key.Right:
			case Gdk.Key.L:
			case Gdk.Key.l:
				if (control && shift)
					FocusCell += VisibleColums - (FocusCell % VisibleColums) - 1;
				else
					FocusCell++;
				break;

			case Gdk.Key.Up:
			case Gdk.Key.K:
			case Gdk.Key.k:
				FocusCell -= VisibleColums;
				break;

			case Gdk.Key.Page_Up:
				FocusCell -= VisibleColums * VisibleRows;
				break;

			case Gdk.Key.Page_Down:
				FocusCell += VisibleColums * VisibleRows;
				break;

			case Gdk.Key.Home:
				FocusCell = 0;
				break;

			case Gdk.Key.End:
				FocusCell = Collection.Count - 1;
				break;

			case Gdk.Key.R:
			case Gdk.Key.r:
				FocusCell = new Random ().Next (0, Collection.Count - 1);
				break;

			case Gdk.Key.space:
				Selection.ToggleCell (FocusCell);
				break;

			case Gdk.Key.Return:
				if (DoubleClicked != null)
					DoubleClicked (this, new BrowsableEventArgs (FocusCell, null));
				break;

			default:
				return false;
			}

			if (shift) {
				if (focus_old != FocusCell && Selection.Contains (focus_old) && Selection.Contains (FocusCell))
					Selection.Remove (FocusCell, focus_old);
				else
					Selection.Add (focus_old, FocusCell);

			} else if (!control) {
				Selection.Clear ();
				Selection.Add (FocusCell);
			}

			ScrollTo (FocusCell);
			return true;
		}

		#endregion
	}
}
