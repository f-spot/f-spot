//
// BrowseablePointerGridView.cs
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

using Gdk;

using FSpot.Core;

namespace FSpot.Widgets
{
    /// <summary>
    ///    This widget displays a photo collection based on an BrowseablePointer. That means, that
    ///    only one photo can be selected at once.
    /// </summary>
    public class BrowseablePointerGridView : CollectionGridView
    {
        public BrowsablePointer Pointer { get; private set; }

        public BrowseablePointerGridView (IntPtr raw) : base (raw)
        {
        }

        public BrowseablePointerGridView (BrowsablePointer pointer)
            : base (pointer.Collection)
        {
            Pointer = pointer;

            Pointer.Changed += (obj, args) => {
                InvalidateCell (args.PreviousIndex);
                InvalidateCell (Pointer.Index);
            };

            AddEvents ((int) EventMask.KeyPressMask
                     | (int) EventMask.ButtonPressMask);

            CanFocus = true;
        }

#region Drawing Methods

        protected override void DrawPhoto (int cell_num, Rectangle cell_area, Rectangle expose_area, bool selected, bool focussed)
        {
            base.DrawPhoto (cell_num, cell_area, expose_area, (Pointer.Index == cell_num), false);
        }

#endregion

#region Override Widget Events

        protected override bool OnButtonPressEvent (EventButton evnt)
        {
            int cell_num = CellAtPosition ((int) evnt.X, (int) evnt.Y);

            GrabFocus ();

            if (cell_num >= 0)
                Pointer.Index = cell_num;

            return true;
        }

        protected override bool OnKeyPressEvent (EventKey evnt)
        {
            bool shift = ModifierType.ShiftMask == (evnt.State & ModifierType.ShiftMask);
            bool control = ModifierType.ControlMask == (evnt.State & ModifierType.ControlMask);

            switch (evnt.Key) {
            case Gdk.Key.Down:
            case Gdk.Key.J:
            case Gdk.Key.j:
                Pointer.Index = Math.Min (Pointer.Collection.Count - 1, Pointer.Index + VisibleColums);
                break;

            case Gdk.Key.Left:
            case Gdk.Key.H:
            case Gdk.Key.h:
                if (control && shift)
                    Pointer.Index -= Pointer.Index % VisibleColums;
                else
                    Pointer.MovePrevious ();
                break;

            case Gdk.Key.Right:
            case Gdk.Key.L:
            case Gdk.Key.l:
                if (control && shift)
                    Pointer.Index = Math.Min (Pointer.Collection.Count - 1,
                                              Pointer.Index + VisibleColums - (Pointer.Index % VisibleColums) - 1);
                else
                    Pointer.MoveNext ();
                break;

            case Gdk.Key.Up:
            case Gdk.Key.K:
            case Gdk.Key.k:
                Pointer.Index = Math.Max (0, Pointer.Index - VisibleColums);
                break;

            case Gdk.Key.Page_Up:
                Pointer.Index = Math.Max (0, Pointer.Index - VisibleColums);
                break;

            case Gdk.Key.Page_Down:
                Pointer.Index = Math.Min (Pointer.Collection.Count - 1, Pointer.Index + VisibleColums * VisibleRows);
                break;

            case Gdk.Key.Home:
                Pointer.MoveFirst ();
                break;

            case Gdk.Key.End:
                Pointer.MoveLast ();
                break;

            default:
                return false;
            }

            ScrollTo (Pointer.Index);
            return true;
        }

#endregion
    }
}

