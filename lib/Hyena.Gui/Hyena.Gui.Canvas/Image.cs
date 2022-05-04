//
// Image.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Gui.Canvas
{
	public class Image : CanvasItem
	{
		public Image ()
		{
		}

		protected override void ClippedRender (Cairo.Context cr)
		{
			Brush brush = Background;
			if (!brush.IsValid) {
				return;
			}

			double x = double.IsNaN (brush.Width)
				? 0
				: (RenderSize.Width - brush.Width) * XAlign;

			double y = double.IsNaN (brush.Height)
				? 0
				: (RenderSize.Height - brush.Height) * YAlign;

			cr.Rectangle (0, 0, RenderSize.Width, RenderSize.Height);
			cr.ClipPreserve ();

			if (x != 0 || y != 0) {
				cr.Translate (x, y);
			}

			cr.Antialias = Cairo.Antialias.None;
			brush.Apply (cr);
			cr.Fill ();
		}

		public double XAlign { get; set; }
		public double YAlign { get; set; }
	}
}
