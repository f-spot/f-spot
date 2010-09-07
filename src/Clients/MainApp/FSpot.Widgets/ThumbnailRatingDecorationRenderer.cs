/*
 * ThumbnailRatingDecorationRenderer.cs
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
    ///    Renders the Rating of a photo as stars to the top left of the thumbnail.
    /// </summary>
    public class ThumbnailRatingDecorationRenderer : ThumbnailDecorationRenderer
    {

#region Private Fields

        RatingRenderer rating_renderer = new RatingRenderer ();

#endregion

#region Constructor

        public ThumbnailRatingDecorationRenderer ()
        {
        }

#endregion

#region Drawing Methods

        public override void Render (Drawable window,
                                     Widget widget,
                                     Rectangle cell_area,
                                     Rectangle expose_area,
                                     StateType cell_state,
                                     IPhoto photo)
        {
            if (photo.Rating > 0) {
                rating_renderer.Value = (int) photo.Rating;

                using (var rating_pixbuf = rating_renderer.RenderPixbuf ()) {
                    rating_pixbuf.RenderToDrawable (window, widget.Style.WhiteGC,
                                                    0, 0, cell_area.X, cell_area.Y,
                                                    -1, -1, RgbDither.None, 0, 0);
                }
            }
        }

#endregion

    }
}
