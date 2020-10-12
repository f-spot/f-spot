//
// ThumbnailTextCaptionRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;
using Gdk;

using Hyena.Gui;

using FSpot.Core;

namespace FSpot.Widgets
{
    /// <summary>
    ///    Class to provide a single text line rendered as caption to a thumbnail.
    /// </summary>
    public abstract class ThumbnailTextCaptionRenderer : ThumbnailCaptionRenderer
    {
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
            string text = GetRenderText (photo);

            var layout = new Pango.Layout (widget.PangoContext);
            layout.SetText (text);

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

        protected abstract string GetRenderText (IPhoto photo);

#endregion

    }
}
