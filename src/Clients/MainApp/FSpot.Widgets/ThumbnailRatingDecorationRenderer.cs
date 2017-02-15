//
// ThumbnailRatingDecorationRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
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
		readonly RatingRenderer rating_renderer = new RatingRenderer ();

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
