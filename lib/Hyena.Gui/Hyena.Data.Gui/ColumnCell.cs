//
// ColumnCell.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using Gtk;
using Cairo;

using Hyena.Gui.Canvas;
using Hyena.Data.Gui.Accessibility;

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
