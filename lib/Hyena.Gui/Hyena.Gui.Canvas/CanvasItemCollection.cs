//
// CanvasItemCollection.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace Hyena.Gui.Canvas
{
    public class CanvasItemCollection : IEnumerable<CanvasItem>
    {
        private CanvasItem parent;
        private List<CanvasItem> children = new List<CanvasItem> ();

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

        private void Unparent (CanvasItem child)
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


