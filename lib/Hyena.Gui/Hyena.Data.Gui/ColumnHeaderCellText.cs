//
// ColumnHeaderCellText.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena.Data.Gui.Accessibility;

namespace Hyena.Data.Gui
{
	public class ColumnHeaderCellText : ColumnCellText, IHeaderCell
	{
		public new delegate Column DataHandler ();

		DataHandler data_handler;
		bool has_sort;

		public ColumnHeaderCellText (DataHandler data_handler) : base (null, true)
		{
			UseMarkup = true;
			this.data_handler = data_handler;
		}

		public override Atk.Object GetAccessible (ICellAccessibleParent parent)
		{
			return new ColumnHeaderCellTextAccessible (BoundObject, this, parent);
		}

		public override void Render (CellContext context, double cellWidth, double cellHeight)
		{
			if (data_handler == null) {
				return;
			}

			if (!has_sort) {
				base.Render (context, cellWidth, cellHeight);
				return;
			}

			var arrow_alloc = new Gdk.Rectangle ();
			arrow_alloc.Width = (int)(cellHeight / 3.0);
			arrow_alloc.Height = (int)((double)arrow_alloc.Width / 1.6);
			arrow_alloc.X = (int)cellWidth - arrow_alloc.Width - (int)Padding.Left;
			arrow_alloc.Y = ((int)cellHeight - arrow_alloc.Height) / 2;

			double textWidth = arrow_alloc.X - Padding.Left;
			if (textWidth > 0) {
				base.Render (context, textWidth, cellHeight);
			}

			SortType sort_type = ((ISortableColumn)data_handler ()).SortType;
			if (sort_type != SortType.None) {
				context.Theme.DrawArrow (context.Context, arrow_alloc, sort_type);
			}
		}

		protected override string GetText (object obj)
		{
			return data_handler ().Title;
		}

		public bool HasSort {
			get { return has_sort; }
			set { has_sort = value; }
		}

		public static int GetArrowWidth (int headerHeight)
		{
			return (int)(headerHeight / 3.0) + 4;
		}
	}
}
