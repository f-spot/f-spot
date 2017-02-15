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
