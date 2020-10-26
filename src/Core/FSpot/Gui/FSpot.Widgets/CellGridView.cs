//
// CellGridView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;

namespace FSpot.Widgets
{
	/// <summary>
	/// This class provides the base functionality for displaying cells in a grid. The
	/// paramters to set up the grid are gathered by abstract properties which must be
	/// implemented by a subclass.
	/// </summary>
	public abstract class CellGridView : Gtk.Layout
	{
		protected CellGridView (IntPtr raw) : base (raw)
		{
		}

		protected CellGridView () : base (null, null)
		{
		}

		#region Abstract Layout Description

		/// <summary>
		///    Must return the width which each cell should have.
		/// </summary>
		protected abstract int MinCellWidth { get; }

		/// <summary>
		///    Must return the height which each cell should have.
		/// </summary>
		protected abstract int MinCellHeight { get; }

		/// <summary>
		///    Must return the number of cells which should be displayed.
		/// </summary>
		protected abstract int CellCount { get; }

		#endregion

		#region Abstract Drawing Functions

		/// <summary>
		///   The function is called to draw a Cell.
		/// </summary>
		protected abstract void DrawCell (int cellNum, Rectangle cellArea, Rectangle exposeArea);

		/// <summary>
		///    The function is called to preload a cell.
		/// </summary>
		protected abstract void PreloadCell (int cellNum);

		#endregion

		#region Private Layout Values

		/// <summary>
		///    The number of cells per row (columns).
		/// </summary>
		protected int CellsPerRow { get; private set; }

		/// <summary>
		///    The width of each cell. It is set, when the layout is updated. The
		///    property <see cref="MinCellWidth"/> is only used when the layout is updated.
		/// </summary>
		protected int CellWidth { get; private set; }

		/// <summary>
		///    The height of each cell. It is set, when the layout is updated. The
		///    property <see cref="MinCellHeight"/> is only used when the layout is updated.
		/// </summary>
		protected int CellHeight { get; private set; }

		/// <summary>
		///    The total number of cells the layout is computed with. It is set, when the
		///    layout is updated. The property <see cref="CellCount"/> is only used when
		///    the layout is updated.
		/// </summary>
		int cellCount;

		/// <summary>
		///    The number of rows which are needed to display all cells.
		/// </summary>
		int totalRows;

		/// <summary>
		///    The border size the current layout is computed with.
		/// </summary>
		int borderSize = 6;

		/// <summary>
		///    The maximal number of columns.
		/// </summary>
		int maxColumns = -1;

		// preserve the scroll postion when possible
		bool scroll;
		double scrollValue;

		// suppress scroll is currently not used. where do we need it?
		bool suppressScroll;

		#endregion

		#region Public Layout Properties

		public int MaxColumns {
			get => maxColumns;
			set {
				maxColumns = value;
				QueueResize ();
			}
		}

		public int BorderSize {
			get => borderSize;
			set {
				if (value < 0)
					throw new ArgumentException (null, nameof (value));

				borderSize = value;
				QueueResize ();
			}
		}

		public int VisibleRows { get; private set; }

		public int VisibleColums => CellsPerRow;

		#endregion

		#region Public Methods

		public int CellAtPosition (Point p)
		{
			return CellAtPosition (p.X, p.Y);
		}

		public int CellAtPosition (int x, int y)
		{
			return CellAtPosition (x, y, true);
		}

		public int CellAtPosition (int x, int y, bool cropVisible)
		{
			if (cropVisible
				&& ((y < (int)Vadjustment.Value || y > (int)Vadjustment.Value + Allocation.Height)
				|| (x < (int)Hadjustment.Value || x > (int)Hadjustment.Value + Allocation.Width)))
				return -1;

			if (x < borderSize || x >= borderSize + CellsPerRow * CellWidth)
				return -1;

			if (y < borderSize || y >= borderSize + (cellCount / CellsPerRow + 1) * CellHeight)
				return -1;

			int column = (int)((x - borderSize) / CellWidth);
			int row = (int)((y - borderSize) / CellHeight);

			int cell_num = column + row * CellsPerRow;
			if (cell_num >= cellCount)
				return -1;

			return cell_num;
		}

		public int TopLeftVisibleCell ()
		{
			// TODO: Where does the 8 come from?
			return CellAtPosition (borderSize, (int)(Vadjustment.Value + Allocation.Height * (Vadjustment.Value / Vadjustment.Upper)) + borderSize + 8);
		}

		public void CellPosition (int cellNum, out int x, out int y)
		{
			// TODO: compare the values with the ones in GetCellCenter.
			if (CellsPerRow == 0) {
				x = 0;
				y = 0;
				return;
			}

			int col = cellNum % CellsPerRow;
			int row = cellNum / CellsPerRow;

			x = col * CellWidth + borderSize;
			y = row * CellHeight + borderSize;
		}

		public void CellCenter (int cellNum, out int x, out int y)
		{
			// TODO: compare the values with the ones in GetCellPosition.
			if (cellNum == -1) {
				x = -1;
				y = -1;
			}

			CellPosition (cellNum, out x, out y);

			x += CellWidth / 2;
			y += CellHeight / 2;
		}

		public Rectangle CellBounds (int cellNum)
		{
			Rectangle bounds;

			CellPosition (cellNum, out bounds.X, out bounds.Y);

			bounds.Width = CellWidth;
			bounds.Height = CellHeight;

			return bounds;
		}

		public IEnumerable<int> CellsInRect (Rectangle area)
		{
			if (CellWidth <= 0 || CellHeight <= 0) {
				yield break;
			}

			int startCellColumn = Math.Max (0, (area.X - borderSize) / CellWidth);
			int startCellRow = Math.Max (0, (area.Y - borderSize) / CellHeight);

			int endCellColumn = Math.Max (0, (area.X + area.Width - borderSize) / CellWidth);
			int endCellRow = Math.Max (0, (area.Y + area.Height - borderSize) / CellHeight);

			for (int cellRow = startCellRow; cellRow <= endCellRow; cellRow++) {

				for (int cellColumn = startCellColumn; cellColumn <= endCellColumn; cellColumn++) {

					int cellNum = cellColumn + cellRow * CellsPerRow;

					if (cellNum < cellCount)
						yield return cellNum;
				}
			}
		}

		public void ScrollTo (int cellNum)
		{
			ScrollTo (cellNum, true);
		}

		public void ScrollTo (int cellNum, bool center)
		{
			if (!IsRealized)
				return;

			Adjustment adjustment = Vadjustment;

			CellPosition (cellNum, out var x, out var y);

			if (center)
				y += CellHeight / 2 - Allocation.Height / 2;

			// the maximal possible adjustment value
			// (otherwise, we are scrolling to far ...)
			int max = (int)(Height - Allocation.Height);

			adjustment.Value = Math.Min (y, max);
			adjustment.ChangeValue ();
		}

		public void InvalidateCell (int cellNum)
		{
			Rectangle cellArea = CellBounds (cellNum);

			// FIXME where are we computing the bounds incorrectly
			cellArea.Width -= 1;
			cellArea.Height -= 1;

			var visible = new Rectangle ((int)Hadjustment.Value, (int)Vadjustment.Value, Allocation.Width, Allocation.Height);
			if (BinWindow != null && cellArea.Intersect (visible, out var intersection))
				BinWindow.InvalidateRect (intersection, false);
		}

		#endregion

		#region Event Handlers

		[GLib.ConnectBefore]
		void HandleAdjustmentValueChanged (object sender, EventArgs args)
		{
			Scroll ();
		}

		#endregion

		#region Determine Layout

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			scrollValue = (Vadjustment.Value) / (Vadjustment.Upper);
			scroll = !suppressScroll;
			suppressScroll = false;
			UpdateLayout (allocation);

			base.OnSizeAllocated (allocation);
		}

		protected override void OnScrollAdjustmentsSet (Adjustment hadjustment, Adjustment vadjustment)
		{
			base.OnScrollAdjustmentsSet (hadjustment, vadjustment);

			if (vadjustment != null)
				vadjustment.ValueChanged += HandleAdjustmentValueChanged;
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			foreach (Rectangle area in args.Region.GetRectangles ()) {
				DrawAllCells (area);
			}
			return base.OnExposeEvent (args);
		}

		void UpdateLayout ()
		{
			UpdateLayout (Allocation);
		}

		void UpdateLayout (Rectangle allocation)
		{
			// get the basic values for the layout ...
			CellWidth = MinCellWidth;
			CellHeight = MinCellHeight;
			cellCount = CellCount;

			// ... and compute the remaining ones.
			int available_width = allocation.Width - 2 * borderSize;
			int available_height = allocation.Height - 2 * borderSize;

			CellsPerRow = Math.Max ((int)(available_width / CellWidth), 1);
			if (MaxColumns > 0)
				CellsPerRow = Math.Min (MaxColumns, CellsPerRow);

			CellWidth += (available_width - CellsPerRow * CellWidth) / CellsPerRow;

			VisibleRows = (int)Math.Max (available_height / CellHeight, 1);

			totalRows = cellCount / CellsPerRow;
			if (cellCount % CellsPerRow != 0)
				totalRows++;

			int height = totalRows * CellHeight + 2 * borderSize;

			Vadjustment.StepIncrement = CellHeight;
			int x = (int)(Hadjustment.Value);
			int y = (int)(height * scrollValue);
			SetSize (x, y, (int)allocation.Width, (int)height);
		}

		void SetSize (int x, int y, int width, int height)
		{
			Hadjustment.Upper = Math.Max (Allocation.Width, width);
			Vadjustment.Upper = Math.Max (Allocation.Height, height);

			bool xchange = scroll && (int)(Hadjustment.Value) != x;
			bool ychange = scroll && (int)(Vadjustment.Value) != y;

			// reset scroll
			scroll = false;

			if (IsRealized)
				BinWindow.FreezeUpdates ();

			if (xchange || ychange) {
				if (IsRealized)
					BinWindow.MoveResize (-x, -y, (int)(Hadjustment.Upper), (int)(Vadjustment.Upper));
				Vadjustment.Value = y;
				Hadjustment.Value = x;
			}

			if (Width != Allocation.Width || Height != Allocation.Height)
				SetSize ((uint)Allocation.Width, (uint)height);

			if (xchange || ychange) {
				Vadjustment.ChangeValue ();
				Hadjustment.ChangeValue ();
			}

			if (IsRealized) {
				BinWindow.ThawUpdates ();
				BinWindow.ProcessUpdates (true);
			}
		}

		void DrawAllCells (Rectangle area)
		{
			foreach (var cellNum in CellsInRect (area)) {
				DrawCell (cellNum, CellBounds (cellNum), area);
			}
		}

		// The first pixel line that is currently on the screen (i.e. in the current
		// scroll region).  Used to compute the area that went offscreen in the "changed"
		// signal handler for the vertical GtkAdjustment.
		int y_offset;
		int x_offset;

		void Scroll ()
		{
			int ystep = (int)(Vadjustment.Value - y_offset);
			int xstep = (int)(Hadjustment.Value - x_offset);

			if (xstep > 0)
				xstep = Math.Max (xstep, Allocation.Width);
			else
				xstep = Math.Min (xstep, -Allocation.Width);

			if (ystep > 0)
				ystep = Math.Max (ystep, Allocation.Height);
			else
				ystep = Math.Min (ystep, -Allocation.Height);

			Rectangle area;

			using var offscreen = new Region ();
			/*
            Log.Debug ("step ({0}, {1}) allocation ({2},{3},{4},{5})",
                    xstep, ystep, Hadjustment.Value, Vadjustment.Value,
                    Allocation.Width, Allocation.Height);
            */
			/*
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 4 * xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + 4 * ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 3 * xstep), 0),
                    Math.Max ((int) (Vadjustment.Value + 3 * ystep), 0),
                    Allocation.Width,
                    Allocation.Height);
            offscreen.UnionWithRect (area);
            */
			area = new Rectangle (Math.Max ((int)(Hadjustment.Value + 2 * xstep), 0),
					Math.Max ((int)(Vadjustment.Value + 2 * ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			area = new Rectangle (Math.Max ((int)(Hadjustment.Value + xstep), 0),
					Math.Max ((int)(Vadjustment.Value + ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			area = new Rectangle ((int)Hadjustment.Value,
					(int)Vadjustment.Value,
					Allocation.Width,
					Allocation.Height);

			// always load the onscreen area last to make sure it
			// is first in the loading
			var onscreen = Region.Rectangle (area);
			offscreen.Subtract (onscreen);

			PreloadRegion (offscreen, ystep);
			Preload (area, false);

			y_offset = (int)Vadjustment.Value;
			x_offset = (int)Hadjustment.Value;
		}

		void PreloadRegion (Region region, int step)
		{
			var rects = region.GetRectangles ();

			if (step < 0)
				Array.Reverse (rects);

			foreach (Rectangle preload in rects) {
				Preload (preload, false);
			}
		}

		void Preload (Rectangle area, bool back)
		{
			if (CellsPerRow == 0)
				return;

			int start_cell_column = Math.Max ((area.X - borderSize) / CellWidth, 0);
			int start_cell_row = Math.Max ((area.Y - borderSize) / CellHeight, 0);
			int start_cell_num = start_cell_column + start_cell_row * CellsPerRow;

			int end_cell_column = Math.Max ((area.X + area.Width - borderSize) / CellWidth, 0);
			int end_cell_row = Math.Max ((area.Y + area.Height - borderSize) / CellHeight, 0);

			int i;

			int cols = end_cell_column - start_cell_column + 1;
			int rows = end_cell_row - start_cell_row + 1;
			int len = rows * cols;
			int scell = start_cell_num;
			int ecell = scell + len;
			if (scell > cellCount - len) {
				ecell = cellCount;
				scell = Math.Max (0, scell - len);
			} else
				ecell = scell + len;

			int mid = (ecell - scell) / 2;
			for (i = 0; i < mid; i++) {

				// The order of Preloading is kept from the previous version, because it provides
				// smooth appearance (alternating for begin and end of the viewport) of the cells.
				// Maybe, this can be done better in a subclass ? (e.g. by calling a PreloadCells
				// with an Array/Enumeration of all cells to be preloaded, or with lower and upper
				// bound of cells to be preloaded)
				int cell = back ? ecell - i - 1 : scell + mid + i;
				PreloadCell (cell);

				cell = back ? scell + i : scell + mid - i - 1;
				PreloadCell (cell);
			}
		}

		#endregion

	}
}

