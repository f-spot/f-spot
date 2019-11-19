//
// ImageView_Panning.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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

using Gdk;

namespace FSpot.Widgets
{
    public partial class ImageView
    {
        #region Panning

        /// <summary>
        ///     Whether or not the user is currently performing a pan motion (dragging with the middle mouse button).
        /// </summary>
        public bool InPanMotion { get; private set; }

        Point pan_anchor = new Point (0, 0);

        bool OnPanButtonPressEvent (EventButton evnt)
        {
            if (evnt.Button != 2) {
                // Restrict to middle mouse button.
                return false;
            }
            
            System.Diagnostics.Debug.Assert (!InPanMotion);
            InPanMotion = true;
            
            // Track starting point of panning movement.
            pan_anchor.X = (int)evnt.X;
            pan_anchor.Y = (int)evnt.Y;
            
            // Set to crosshair pointer
            GdkWindow.Cursor = new Cursor (CursorType.Fleur);
            return true;
        }

        bool OnPanMotionNotifyEvent (EventMotion evnt)
        {
            if (!InPanMotion) {
                return false;
            }
            
            // Calculate the direction of the panning, scroll accordingly.
            int pan_x = pan_anchor.X - (int)evnt.X;
            int pan_y = pan_anchor.Y - (int)evnt.Y;
            ScrollBy (pan_x, pan_y);
            
            // Reset starting point.
            pan_anchor.X = (int)evnt.X;
            pan_anchor.Y = (int)evnt.Y;
            return true;
        }

        bool OnPanButtonReleaseEvent (EventButton evnt)
        {
            if (evnt.Button != 2) {
                // Restrict to middle mouse button.
                return false;
            }
            
            System.Diagnostics.Debug.Assert (InPanMotion);
            InPanMotion = false;
            
            // Reset cursor
            GdkWindow.Cursor = null;
            return true;
        }
        
        #endregion
        
    }
}

