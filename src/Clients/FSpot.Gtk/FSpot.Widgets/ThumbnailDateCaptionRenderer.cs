//
// ThumbnailDateCaptionRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Core;

using Gdk;

using Gtk;

using Hyena.Gui;

namespace FSpot.Widgets
{
	/// <summary>
	///    Renders a text caption with the date of the photo. This class is not based on
	///    TextCaptionRenderer, because it uses caching of the dates.
	/// </summary>
	public class ThumbnailDateCaptionRenderer : ThumbnailCaptionRenderer
	{
		readonly Dictionary<string, Pango.Layout> cache = new Dictionary<string, Pango.Layout> ();

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

			if (!cache.TryGetValue (date_text, out var layout)) {
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
