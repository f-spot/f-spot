//
// ColumnCellAccessible.cs
//
// Author:
//   Eitan Isaacson <eitan@ascender.com>
//
// Copyright (C) 2009 Eitan Isaacson.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Gui.Accessibility
{
	public class ColumnCellAccessible : Atk.Object, Atk.ComponentImplementor
	{
		protected ColumnCell cell;
		protected object bound_object;
		ICellAccessibleParent cell_parent;

		public ColumnCellAccessible (object bound_object, ColumnCell cell, ICellAccessibleParent parent)
		{
			Role = Atk.Role.TableCell;
			this.bound_object = bound_object;
			this.cell = cell;
			cell_parent = parent;
			Parent = (Atk.Object)parent;
		}

		protected override Atk.StateSet OnRefStateSet ()
		{
			Atk.StateSet states = base.OnRefStateSet ();
			states.AddState (Atk.StateType.Transient);
			states.AddState (Atk.StateType.Focusable);
			states.AddState (Atk.StateType.Enabled);
			states.AddState (Atk.StateType.Sensitive);
			states.AddState (Atk.StateType.Visible);

			if (cell_parent.IsCellShowing (this))
				states.AddState (Atk.StateType.Showing);

			if (cell_parent.IsCellFocused (this))
				states.AddState (Atk.StateType.Focused);

			if (cell_parent.IsCellSelected (this))
				states.AddState (Atk.StateType.Selected);

			if (cell_parent.IsCellActive (this))
				states.AddState (Atk.StateType.Active);

			return states;
		}

		protected override int OnGetIndexInParent ()
		{
			return cell_parent.GetCellIndex (this);
		}

		public double Alpha {
			get { return 1.0; }
		}

		public bool SetSize (int w, int h)
		{
			return false;
		}

		public bool SetPosition (int x, int y, Atk.CoordType coordType)
		{
			return false;
		}

		public bool SetExtents (int x, int y, int w, int h, Atk.CoordType coordType)
		{
			return false;
		}

		public void RemoveFocusHandler (uint handlerId)
		{
		}

		public bool GrabFocus ()
		{
			return false;
		}

		public void GetSize (out int w, out int h)
		{
			Gdk.Rectangle rectangle = cell_parent.GetCellExtents (this, Atk.CoordType.Screen);
			w = rectangle.Width;
			h = rectangle.Height;
		}

		public void GetPosition (out int x, out int y, Atk.CoordType coordType)
		{
			Gdk.Rectangle rectangle = cell_parent.GetCellExtents (this, coordType);

			x = rectangle.X;
			y = rectangle.Y;
		}

		public void GetExtents (out int x, out int y, out int w, out int h, Atk.CoordType coordType)
		{
			Gdk.Rectangle rectangle = cell_parent.GetCellExtents (this, coordType);

			x = rectangle.X;
			y = rectangle.Y;
			w = rectangle.Width;
			h = rectangle.Height;
		}

		public virtual Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
		{
			return null;
		}

		public bool Contains (int x, int y, Atk.CoordType coordType)
		{
			return false;
		}

		public uint AddFocusHandler (Atk.FocusHandler handler)
		{
			return 0;
		}

		public virtual void Redrawn ()
		{
		}
	}
}
