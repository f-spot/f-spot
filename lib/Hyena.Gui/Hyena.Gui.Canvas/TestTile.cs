//
// TestTile.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using Cairo;

namespace Hyena.Gui.Canvas
{
	public class TestTile : CanvasItem
	{
		static Random rand = new Random ();
		Color color;
		bool color_set;

		bool change_on_render = true;
		public bool ChangeOnRender {
			get { return change_on_render; }
			set { change_on_render = value; }
		}

		public TestTile ()
		{
		}

		protected override void ClippedRender (Context cr)
		{
			if (!color_set || ChangeOnRender) {
				color = CairoExtensions.RgbToColor ((uint)rand.Next (0, 0xffffff));
				color_set = true;
			}

			CairoExtensions.RoundedRectangle (cr, 0, 0, RenderSize.Width, RenderSize.Height, 5);
			cr.SetSourceColor (color);
			cr.Fill ();
		}
	}
}
