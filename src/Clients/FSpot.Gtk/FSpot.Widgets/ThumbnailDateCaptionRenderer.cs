//
// ThumbnailDateCaptionRenderer.cs
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
		readonly Dictionary<string, Pango.Layout> cache = new Dictionary<string, Pango.Layout> ();

		public override int GetHeight (Widget widget, int width)
		{
			return widget.Style.FontDescription.MeasureTextHeight (widget.PangoContext);
		}

		public override void Render (Drawable window,
									 Widget widget,
									 Rectangle cellArea,
									 Rectangle exposeArea,
									 StateType cellState,
									 IPhoto photo)
		{
			string date_text = null;

			if (photo is IInvalidPhotoCheck check && check.IsInvalid)
				return;

			if (cellArea.Width > 200) {
				date_text = photo.UtcTime.ToString ();
			} else {
				date_text = photo.UtcTime.ToShortDateString ();
			}

			if (!cache.TryGetValue (date_text, out var layout)) {
				layout = new Pango.Layout (widget.PangoContext);
				layout.SetText (date_text);

				cache.Add (date_text, layout);
			}

			Rectangle layout_bounds;
			layout.GetPixelSize (out layout_bounds.Width, out layout_bounds.Height);

			layout_bounds.Y = cellArea.Y;
			layout_bounds.X = cellArea.X + (cellArea.Width - layout_bounds.Width) / 2;

			if (layout_bounds.IntersectsWith (exposeArea)) {
				Style.PaintLayout (widget.Style, window, cellState,
								   true, exposeArea, widget, "IconView",
								   layout_bounds.X, layout_bounds.Y,
								   layout);
			}
		}
	}
}
