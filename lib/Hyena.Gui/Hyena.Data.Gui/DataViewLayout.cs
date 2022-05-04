//
// DataViewLayout.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
	public abstract class DataViewLayout
	{
		List<CanvasItem> children = new List<CanvasItem> ();
		protected List<CanvasItem> Children {
			get { return children; }
		}

		Dictionary<CanvasItem, int> model_indices = new Dictionary<CanvasItem, int> ();

		public IListModel Model { get; set; }

		protected CanvasManager CanvasManager;

		ListViewBase view;
		public ListViewBase View {
			get { return view; }
			set {
				view = value;
				CanvasManager = new CanvasManager (view);
			}
		}

		public Rect ActualAllocation { get; protected set; }
		public Size VirtualSize { get; protected set; }
		public Size ChildSize { get; protected set; }
		public int XPosition { get; protected set; }
		public int YPosition { get; protected set; }

		public int ChildCount {
			get { return Children.Count; }
		}

		public DataViewLayout ()
		{
		}

		public CanvasItem this[int index] {
			get { return Children[index]; }
		}

		public void UpdatePosition (int x, int y)
		{
			XPosition = x;
			YPosition = y;
			InvalidateChildLayout (false);
		}

		public void ModelUpdated ()
		{
			InvalidateVirtualSize ();
			InvalidateChildLayout ();
		}

		public virtual void Allocate (Rect actualAllocation)
		{
			ActualAllocation = actualAllocation;

			InvalidateChildSize ();
			InvalidateChildCollection ();
			InvalidateVirtualSize ();
			InvalidateChildLayout ();
		}

		public virtual CanvasItem FindChildAtPoint (Point point)
		{
			return Children.Find (child => child.Allocation.Contains (
				ActualAllocation.X + point.X, ActualAllocation.Y + point.Y));
		}

		public virtual CanvasItem FindChildAtModelRowIndex (int modelRowIndex)
		{
			return Children.Find (child => GetModelIndex (child) == modelRowIndex);
		}

		protected abstract void InvalidateChildSize ();
		protected abstract void InvalidateVirtualSize ();
		protected abstract void InvalidateChildCollection ();
		protected void InvalidateChildLayout ()
		{
			InvalidateChildLayout (true);
		}

		protected virtual void InvalidateChildLayout (bool arrange)
		{
			model_indices.Clear ();
		}

		protected void SetModelIndex (CanvasItem item, int index)
		{
			model_indices[item] = index;
		}

		public int GetModelIndex (CanvasItem item)
		{
			return model_indices.TryGetValue (item, out var i) ? i : -1;
		}

		protected Rect GetChildVirtualAllocation (Rect childAllocation)
		{
			return new Rect () {
				X = childAllocation.X - ActualAllocation.X,
				Y = childAllocation.Y - ActualAllocation.Y,
				Width = childAllocation.Width,
				Height = childAllocation.Height
			};
		}
	}
}
