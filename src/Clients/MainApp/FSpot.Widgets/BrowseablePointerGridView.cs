/*
 * BrowseablePointerGridView.cs
 *
 * Author(s)
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;

using Gdk;
using Gtk;

using FSpot.Core;


namespace FSpot.Widgets
{
    /// <summary>
    ///    This widget displays a photo collection based on an BrowseablePointer. That means, that
    ///    only one photo can be selected at once.
    /// </summary>
    public class BrowseablePointerGridView : CollectionGridView
    {

#region Public Properties

        public BrowsablePointer Pointer {
            get; private set;
        }

#endregion

#region Constructors

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

#endregion

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

