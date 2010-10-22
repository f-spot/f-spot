/*
 * ThumbnailDateCaptionRenderer.cs
 *
 * Author(s)
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

using Gtk;
using Gdk;

using Hyena.Gui;

using FSpot.Core;


namespace FSpot.Widgets
{
    /// <summary>
    ///    Renders a text caption with the date of the photo. This class is not based on
    ///    TextCaptionRenderer, because it uses caching of the dates.
    /// </summary>
    public class ThumbnailDateCaptionRenderer : ThumbnailCaptionRenderer
    {

#region Private Fields

        private Dictionary <string, Pango.Layout> cache = new Dictionary <string, Pango.Layout> ();

#endregion

#region Constructor

        public ThumbnailDateCaptionRenderer ()
        {
        }

#endregion

#region Drawing Methods

        public override int GetHeight (Widget widget, int width)
        {
            return widget.Style.FontDescription.MeasureTextHeight (widget.PangoContext);
        }

        public override void Render (Drawable window,
                                     Widget widget,
                                     Rectangle cell_area,
                                     Rectangle expose_area,
                                     StateType cell_state,
                                     IPhoto photo)
        {
            string date_text = null;

            if (photo is IInvalidPhotoCheck && (photo as IInvalidPhotoCheck).IsInvalid)
                return;

            if (cell_area.Width > 200) {
                date_text = photo.Time.ToString ();
            } else {
                date_text = photo.Time.ToShortDateString ();
            }

            Pango.Layout layout = null;
            if ( ! cache.TryGetValue (date_text, out layout)) {
                layout = new Pango.Layout (widget.PangoContext);
                layout.SetText (date_text);

                cache.Add (date_text, layout);
            }

            Rectangle layout_bounds;
            layout.GetPixelSize (out layout_bounds.Width, out layout_bounds.Height);

            layout_bounds.Y = cell_area.Y;
            layout_bounds.X = cell_area.X + (cell_area.Width - layout_bounds.Width) / 2;

            if (layout_bounds.IntersectsWith (expose_area)) {
                Style.PaintLayout (widget.Style, window, cell_state,
                                   true, expose_area, widget, "IconView",
                                   layout_bounds.X, layout_bounds.Y,
                                   layout);
            }
        }

#endregion

    }
}
