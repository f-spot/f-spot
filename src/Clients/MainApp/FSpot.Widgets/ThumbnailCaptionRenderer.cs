/*
 * ThumbnailCaptionRenderer.cs
 *
 * Author(s)
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;

using Gtk;
using Gdk;

using FSpot.Core;


namespace FSpot.Widgets
{
    /// <summary>
    ///    Renders a caption below a thumbnail. It must compute the height needed for
    ///    the annotation.
    /// </summary>
    public abstract class ThumbnailCaptionRenderer
    {

#region Constructor

        public ThumbnailCaptionRenderer ()
        {
        }

#endregion

#region Drawing Methods

        public abstract int GetHeight (Widget widget, int width);

        public abstract void Render (Drawable window,
                                     Widget widget,
                                     Rectangle cell_area,
                                     Rectangle expose_area,
                                     StateType cell_state,
                                     IPhoto photo);

#endregion

    }
}
