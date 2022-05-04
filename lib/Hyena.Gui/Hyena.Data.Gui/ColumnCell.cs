//
// ColumnCell.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

using Hyena.Data.Gui.Accessibility;
using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
	public abstract class ColumnCell : CanvasItem
	{
		public virtual Atk.Object GetAccessible (ICellAccessibleParent parent)
		{
			return new ColumnCellAccessible (BoundObject, this, parent);
		}

		public virtual string GetTextAlternative (object obj)
		{
			return "";
		}

		public ColumnCell (string property, bool expand)
		{
			Binder = ObjectBinder = new ObjectBinder () { Property = property };
			Expand = expand;
		}

		public ObjectBinder ObjectBinder { get; private set; }

		public object BoundObjectParent {
			get { return ObjectBinder.BoundObjectParent; }
		}

		public string Property {
			get { return ObjectBinder.Property; }
			set { ObjectBinder.Property = value; }
		}

		public virtual void NotifyThemeChange ()
		{
		}

		public virtual Gdk.Size Measure (Widget widget)
		{
			return Gdk.Size.Empty;
		}

		protected override void ClippedRender (CellContext context)
		{
			Render (context, ContentAllocation.Width, ContentAllocation.Height);
		}

		public virtual void Render (CellContext context, double cellWidth, double cellHeight)
		{
			Render (context, context.State, cellWidth, cellHeight);
		}

		public virtual void Render (CellContext context, Gtk.StateType state, double cellWidth, double cellHeight)
		{
		}

		public bool Expand { get; set; }

		public Size? FixedSize { get; set; }

		public override void Arrange ()
		{
		}
	}
}
