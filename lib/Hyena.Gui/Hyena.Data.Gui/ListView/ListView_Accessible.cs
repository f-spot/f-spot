//
// ListView_Accessible.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Eitan Isaacson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Hyena.Data.Gui.Accessibility;

namespace Hyena.Data.Gui
{
	public partial class ListView<T> : ListViewBase
	{
		internal ListViewAccessible<T> accessible;

		static ListView ()
		{
			ListViewAccessibleFactory<T>.Init ();
		}

		public Gdk.Rectangle GetColumnCellExtents (int row, int column)
		{
			return GetColumnCellExtents (row, column, true);
		}

		public Gdk.Rectangle GetColumnCellExtents (int row, int column, bool clip)
		{
			return GetColumnCellExtents (row, column, clip, Atk.CoordType.Window);
		}

		public Gdk.Rectangle GetColumnCellExtents (int row, int column, bool clip, Atk.CoordType coord_type)
		{
			int width = GetColumnWidth (column);
			int height = ChildSize.Height;

			int y = (int)GetViewPointForModelRow (row).Y - VadjustmentValue + ListAllocation.Y;

			int x = ListAllocation.X - HadjustmentValue;
			for (int index = 0; index < column; index++)
				x += GetColumnWidth (index);

			var rectangle = new Gdk.Rectangle (x, y, width, height);

			if (clip && !ListAllocation.Contains (rectangle))
				return new Gdk.Rectangle (int.MinValue, int.MinValue, int.MinValue, int.MinValue);

			if (coord_type == Atk.CoordType.Window)
				return rectangle;

			GdkWindow.GetPosition (out var origin_x, out var origin_y);

			rectangle.X += origin_x;
			rectangle.Y += origin_y;

			return rectangle;
		}

		public Gdk.Rectangle GetColumnHeaderCellExtents (int column, bool clip, Atk.CoordType coord_type)
		{
			if (!HeaderVisible)
				return new Gdk.Rectangle (int.MinValue, int.MinValue, int.MinValue, int.MinValue);
			int width = GetColumnWidth (column);
			int height = HeaderHeight;

			int x = header_rendering_alloc.X - HadjustmentValue + Theme.BorderWidth;
			if (column != 0)
				x += Theme.InnerBorderWidth;
			for (int index = 0; index < column; index++)
				x += GetColumnWidth (index);

			int y = Theme.BorderWidth + header_rendering_alloc.Y;

			var rectangle = new Gdk.Rectangle (x, y, width, height);

			if (coord_type == Atk.CoordType.Window)
				return rectangle;

			GdkWindow.GetPosition (out var origin_x, out var origin_y);

			rectangle.X += origin_x;
			rectangle.Y += origin_y;

			return rectangle;
		}

		public void GetCellAtPoint (int x, int y, Atk.CoordType coord_type, out int row, out int col)
		{
			int origin_x = 0;
			int origin_y = 0;
			if (coord_type == Atk.CoordType.Screen)
				GdkWindow.GetPosition (out origin_x, out origin_y);

			x = x - ListAllocation.X - origin_x;
			y = y - ListAllocation.Y - origin_y;

			Column column = GetColumnAt (x);

			CachedColumn cached_column = GetCachedColumnForColumn (column);

			row = GetModelRowAt (x, y);
			col = cached_column.Index;
		}

		public void InvokeColumnHeaderMenu (int column)
		{
			Gdk.Rectangle rectangle = GetColumnHeaderCellExtents (column, true, Atk.CoordType.Window);
			Column col = ColumnController.Where (c => c.Visible).ElementAtOrDefault (column);
			OnColumnRightClicked (col, rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
		}

		public void ClickColumnHeader (int column)
		{
			Column col = ColumnController.Where (c => c.Visible).ElementAtOrDefault (column);
			OnColumnLeftClicked (col);
		}

		void AccessibleCellRedrawn (int column, int row)
		{
			if (accessible != null) {
				accessible.CellRedrawn (column, row);
			}
		}

	}

	class ListViewAccessibleFactory<T> : Atk.ObjectFactory
	{
		public static void Init ()
		{
			try {
				// Test creating a dummy accessible, which may throw if gobject binding has issues.
				// If it throws, a11y for ListView will not be enabled.
				// (workaround for https://bugzilla.xamarin.com/show_bug.cgi?id=11510)
				new ListViewAccessible<T> (new ListView<T> ());

				new ListViewAccessibleFactory<T> ();
				Atk.Global.DefaultRegistry.SetFactoryType ((GLib.GType)typeof (ListView<T>), (GLib.GType)typeof (ListViewAccessibleFactory<T>));
			} catch (Exception ex) {
				Log.Exception ("Initialization of accessibility support for ListView widgets failed", ex);
			}
		}

		protected override Atk.Object OnCreateAccessible (GLib.Object obj)
		{
			Log.InformationFormat ("Creating Accessible for {0}", obj);
			var accessible = new ListViewAccessible<T> (obj);
			(obj as ListView<T>).accessible = accessible;
			return accessible;
		}

		protected override GLib.GType OnGetAccessibleType ()
		{
			return ListViewAccessible<T>.GType;
		}
	}
}
