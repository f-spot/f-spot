//
// RatingRenderer.cs
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

using Gdk;
using Cairo;

using Hyena.Gui;

using FSpot.Utils;

namespace FSpot.Widgets
{
    public class RatingRenderer : Hyena.Gui.RatingRenderer
    {
        private static int REQUESTED_ICON_SIZE = 16;

#region Shared Pixbufs

        // cache the unscaled pixbufs for all instances
        private static Pixbuf icon_rated;
        private static Pixbuf icon_blank;
        private static Pixbuf icon_hover;

#endregion

#region Access Rating Pixbufs

        protected static Pixbuf IconRated {
            get {
                if (icon_rated == null)
                    icon_rated =
                        GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme,
                                             "rating-rated",
                                             REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

                return icon_rated;
            }
        }

        protected static Pixbuf IconBlank {
            get {
                if (icon_blank == null)
                    icon_blank =
                        GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme,
                                             "rating-blank",
                                             REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

                return icon_blank;
            }
        }

        protected static Pixbuf IconHover {
            get {
                if (icon_hover == null)
                    icon_hover =
                        GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme,
                                             "rating-rated-gray",
                                             REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

                return icon_hover;
            }
        }

#endregion

#region Cache and Access Scaled Rating Pixbufs

        // cache the scaled pixbufs for every instance
        private int scaled_icon_size;
        private Pixbuf scaled_icon_rated;
        private Pixbuf scaled_icon_blank;
        private Pixbuf scaled_icon_hover;

        protected Pixbuf ScaledIconRated {
            get {
                if (scaled_icon_size == Size && scaled_icon_rated != null)
                    return scaled_icon_rated;

                if (scaled_icon_size != Size)
                    ResetCachedPixbufs ();

                scaled_icon_rated = ScaleIcon (IconRated);
                scaled_icon_size = Size;

                return scaled_icon_rated;
            }
        }

        protected Pixbuf ScaledIconBlank {
            get {
                if (scaled_icon_size == Size && scaled_icon_blank != null)
                    return scaled_icon_blank;

                if (scaled_icon_size != Size)
                    ResetCachedPixbufs ();

                scaled_icon_blank = ScaleIcon (IconBlank);
                scaled_icon_size = Size;

                return scaled_icon_blank;
            }
        }

        protected Pixbuf ScaledIconHover {
            get {
                if (scaled_icon_size == Size && scaled_icon_hover != null)
                    return scaled_icon_hover;

                if (scaled_icon_size != Size)
                    ResetCachedPixbufs ();

                scaled_icon_hover = ScaleIcon (IconHover);
                scaled_icon_size = Size;

                return scaled_icon_hover;
            }
        }

        private void ResetCachedPixbufs ()
        {
            if (scaled_icon_rated != null) {
                scaled_icon_rated.Dispose ();
                scaled_icon_rated = null;
            }

            if (scaled_icon_blank != null) {
                scaled_icon_blank.Dispose ();
                scaled_icon_blank = null;
            }

            if (scaled_icon_hover != null) {
                scaled_icon_hover.Dispose ();
                scaled_icon_hover = null;
            }
        }

        private Pixbuf ScaleIcon (Pixbuf icon)
        {
            if (icon.Width > Size) {
                return icon.ScaleSimple (Size, Size, InterpType.Bilinear);
            }

            var scaled_icon = new Pixbuf (Colorspace.Rgb, true, 8, Size, Size);
            scaled_icon.Fill (0xffffff00);

            int x_offset = (Size - icon.Width) / 2;
            int y_offset = (Size - icon.Height) / 2;

            icon.CopyArea (0, 0, icon.Width, icon.Height, scaled_icon, x_offset, y_offset);
            return scaled_icon;
        }

#endregion

#region Constructors / Destructor

        public RatingRenderer ()
        {
        }

        ~RatingRenderer ()
        {
            ResetCachedPixbufs ();
        }

#endregion

#region Drawing Code

        public Pixbuf RenderPixbuf ()
        {
            return RenderPixbuf (false);
        }

        public Pixbuf RenderPixbuf (bool showEmptyStars)
        {
            return RenderPixbuf (showEmptyStars, false, MinRating - 1, 0.0, 0.0, 1.0);
        }

        public Pixbuf RenderPixbuf (bool showEmptyStars, bool isHovering, int hoverValue, double fillOpacity,
                                    double hoverFillOpacity, double strokeOpacity)
        {
            var pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, MaxRating * Size, Size);
            pixbuf.Fill (0xffffff00);

            int x = 0;
            for (int i = MinRating + 1, s = isHovering || showEmptyStars ? MaxRating : Value; i <= s; i++, x += Size) {

                Pixbuf icon = null;

                bool rated = (i <= Value && Value > MinRating);
                bool hover = isHovering &&
                    rated ? (i > hoverValue && hoverValue < Value) : (i <= hoverValue && hoverValue > MinRating);

                // hover
                if (hover) {
                    icon = ScaledIconHover;

                // rated
                } else if (rated) {
                    icon = ScaledIconRated;

                // empty 'star'
                } else {
                    icon = ScaledIconBlank;

                }

                icon.CopyArea (0, 0, icon.Width, icon.Height, pixbuf, x, 0);
            }

            return pixbuf;
        }

#endregion

#region Override Render Code

        public override void Render (Cairo.Context cr, Gdk.Rectangle area, Cairo.Color color, bool showEmptyStars,
                                     bool isHovering, int hoverValue, double fillOpacity, double hoverFillOpacity,
                                     double strokeOpacity)
        {
            if (Value == MinRating && !isHovering && !showEmptyStars) {
                return;
            }

            double x, y;
            ComputePosition (area, out x, out y);

            cr.Translate (0.5, 0.5);

            using (var pixbuf = RenderPixbuf (showEmptyStars, isHovering, hoverValue,
                                              fillOpacity, hoverFillOpacity, strokeOpacity)) {
                using (var surface = CairoExtensions.CreateSurfaceForPixbuf (cr, pixbuf)) {
                    cr.Rectangle (x, y, pixbuf.Width, pixbuf.Height);
                    cr.SetSource (surface, x, y);
                    cr.Fill ();
                }
            }
        }

#endregion

    }
}

