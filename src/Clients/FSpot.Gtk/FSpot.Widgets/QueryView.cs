//
// QueryView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2007 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

//
// QueryView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

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
            ScrollEvent += HandleScrollEvent;
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
            popup.Activate (Toplevel, evnt);
        }

        void HandleScrollEvent(object sender, ScrollEventArgs args)
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
