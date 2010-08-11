using Gdk;
using Gtk;

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

