//
// QueryView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

using Gdk;
using Gtk;

using FSpot.Core;


namespace FSpot.Widgets
{
    public class QueryView : SelectionCollectionGridView
    {
        public QueryView (System.IntPtr raw) : base(raw)
        {
        }

        public QueryView (IBrowsableCollection query) : base(query)
        {
            ScrollEvent += new ScrollEventHandler (HandleScrollEvent);
        }

        protected override bool OnPopupMenu ()
        {
            PhotoPopup popup = new PhotoPopup ();
            popup.Activate ();
            return true;
        }

        protected override void ContextMenu (EventButton evnt, int cell_num)
        {
            PhotoPopup popup = new PhotoPopup ();
            popup.Activate (this.Toplevel, evnt);
        }

        private void HandleScrollEvent(object sender, ScrollEventArgs args)
        {
            // Activated only by Control + ScrollWheelUp/ScrollWheelDown
            if (ModifierType.ControlMask != (args.Event.State & ModifierType.ControlMask))
                return;

            if (args.Event.Direction == ScrollDirection.Up) {
                ZoomIn ();
                // stop event from propagating.
                args.RetVal = true;
            } else if (args.Event.Direction == ScrollDirection.Down ) {
                ZoomOut ();
                args.RetVal = true;
            }
        }
    }
}
