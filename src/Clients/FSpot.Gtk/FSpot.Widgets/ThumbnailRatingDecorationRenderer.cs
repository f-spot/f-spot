//
// ThumbnailRatingDecorationRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

using Gdk;

using Gtk;

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
				rating_renderer.Value = (int)photo.Rating;

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
