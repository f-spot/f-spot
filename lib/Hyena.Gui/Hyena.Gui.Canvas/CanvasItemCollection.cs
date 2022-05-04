//
// CanvasItemCollection.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;

namespace Hyena.Gui.Canvas
{
	public class CanvasItemCollection : IEnumerable<CanvasItem>
	{
		CanvasItem parent;
		List<CanvasItem> children = new List<CanvasItem> ();

		public CanvasItemCollection (CanvasItem parent)
		{
			this.parent = parent;
		}

		public void Add (CanvasItem child)
		{
			if (!children.Contains (child)) {
				children.Add (child);
				child.Parent = parent;
				parent.InvalidateArrange ();
			}
		}

		void Unparent (CanvasItem child)
		{
			child.Parent = null;
		}

		public void Remove (CanvasItem child)
		{
			if (children.Remove (child)) {
				Unparent (child);
				parent.InvalidateArrange ();
			}
		}

		public void Move (CanvasItem child, int position)
		{
			if (children.Remove (child)) {
				children.Insert (position, child);
				parent.InvalidateArrange ();
			}
		}

		public void Clear ()
		{
			foreach (var child in children) {
				Unparent (child);
			}
			children.Clear ();
		}

		public IEnumerator<CanvasItem> GetEnumerator ()
		{
			foreach (var item in children) {
				yield return item;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public CanvasItem this[int index] {
			get { return children[index]; }
		}

		public CanvasItem Parent {
			get { return parent; }
		}

		public int Count {
			get { return children.Count; }
		}
	}
}


