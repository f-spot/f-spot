/*
 * ThumbnailDecorationRenderer.cs
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
    ///    This is a renderer for drawing annotations to a thumbnail. The annotations
    ///    are rendered directly to the thumbnail and no previous size computation is needed.
    /// </summary>
    public abstract class ThumbnailDecorationRenderer
    {

#region Constructor

        public ThumbnailDecorationRenderer ()
        {
        }

#endregion

#region Drawing Methods

        public abstract void Render (Drawable window,
                                     Widget widget,
                                     Rectangle cell_area,
                                     Rectangle expose_area,
                                     StateType cell_state,
                                     IPhoto photo);

#endregion

    }
}
