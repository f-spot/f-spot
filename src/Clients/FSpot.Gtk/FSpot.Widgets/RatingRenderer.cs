//
// RatingRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Settings;
using FSpot.Utils;

using Gdk;

using Hyena.Gui;

namespace FSpot.Widgets
{
	public class RatingRenderer : Hyena.Gui.RatingRenderer
	{
		const int REQUESTED_ICON_SIZE = 16;

		// cache the unscaled pixbufs for all instances
		static Pixbuf iconRated;
		static Pixbuf iconBlank;
		static Pixbuf iconHover;

		protected static Pixbuf IconRated {
			get {
				if (iconRated == null)
					iconRated = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "rating-rated", REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

				return iconRated;
			}
		}

		protected static Pixbuf IconBlank {
			get {
				if (iconBlank == null)
					iconBlank = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "rating-blank", REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

				return iconBlank;
			}
		}

		protected static Pixbuf IconHover {
			get {
				if (iconHover == null)
					iconHover = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, "rating-rated-gray", REQUESTED_ICON_SIZE, (Gtk.IconLookupFlags)0);

				return iconHover;
			}
		}

		// cache the scaled pixbufs for every instance
		int scaledIconSize;
		Pixbuf scaledIconRated;
		Pixbuf scaledIconBlank;
		Pixbuf scaledIconHover;

		protected Pixbuf ScaledIconRated {
			get {
				if (scaledIconSize == Size && scaledIconRated != null)
					return scaledIconRated;

				if (scaledIconSize != Size)
					ResetCachedPixbufs ();

				scaledIconRated = ScaleIcon (IconRated);
				scaledIconSize = Size;

				return scaledIconRated;
			}
		}

		protected Pixbuf ScaledIconBlank {
			get {
				if (scaledIconSize == Size && scaledIconBlank != null)
					return scaledIconBlank;

				if (scaledIconSize != Size)
					ResetCachedPixbufs ();

				scaledIconBlank = ScaleIcon (IconBlank);
				scaledIconSize = Size;

				return scaledIconBlank;
			}
		}

		protected Pixbuf ScaledIconHover {
			get {
				if (scaledIconSize == Size && scaledIconHover != null)
					return scaledIconHover;

				if (scaledIconSize != Size)
					ResetCachedPixbufs ();

				scaledIconHover = ScaleIcon (IconHover);
				scaledIconSize = Size;

				return scaledIconHover;
			}
		}

		void ResetCachedPixbufs ()
		{
			if (scaledIconRated != null) {
				scaledIconRated.Dispose ();
				scaledIconRated = null;
			}

			if (scaledIconBlank != null) {
				scaledIconBlank.Dispose ();
				scaledIconBlank = null;
			}

			if (scaledIconHover != null) {
				scaledIconHover.Dispose ();
				scaledIconHover = null;
			}
		}

		Pixbuf ScaleIcon (Pixbuf icon)
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

		public RatingRenderer ()
		{
		}

		~RatingRenderer ()
		{
			ResetCachedPixbufs ();
		}

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
				bool rated = (i <= Value && Value > MinRating);
				bool hover = isHovering &&
					rated ? (i > hoverValue && hoverValue < Value) : (i <= hoverValue && hoverValue > MinRating);

				Pixbuf icon;
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

		public override void Render (Cairo.Context cr, Rectangle area, Cairo.Color color, bool showEmptyStars,
									 bool isHovering, int hoverValue, double fillOpacity, double hoverFillOpacity,
									 double strokeOpacity)
		{
			if (Value == MinRating && !isHovering && !showEmptyStars)
				return;

			ComputePosition (area, out var x, out var y);

			cr.Translate (0.5, 0.5);

			using var pixbuf = RenderPixbuf (showEmptyStars, isHovering, hoverValue,
											 fillOpacity, hoverFillOpacity, strokeOpacity);
			using var surface = CairoExtensions.CreateSurfaceForPixbuf (cr, pixbuf);

			cr.Rectangle (x, y, pixbuf.Width, pixbuf.Height);
			cr.SetSource (surface, x, y);
			cr.Fill ();
		}
	}
}

