//
// FSpot.Widgets.DissolveTransition.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// Copyright (c) 2009 Novell, Inc.
//
// This is open source software. See COPYING for details.
//

using System;

using Cairo;
using Gdk;

using FSpot.Utils;
using FSpot.Widgets;

using Color = Cairo.Color;

namespace FSpot.Widgets
{
	public class DissolveTransition : CairoTransition
	{
		public DissolveTransition () : base ("Dissolve")
		{
		}

		protected override void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			cr.Color = new Color (0, 0, 0, progress);
			if (next != null) {
				double scale = Math.Min ((double)width/(double)next.Width, (double)height/(double)next.Height);
				cr.Save ();

				cr.Rectangle (0, 0, width, .5 * (height - scale*next.Height));
				cr.Fill ();

				cr.Rectangle (0, height - .5 * (height - scale*next.Height), width, .5 * (height - scale*next.Height));
				cr.Fill ();

				cr.Rectangle (0, 0, .5 * (width - scale*next.Width), height);
				cr.Fill ();

				cr.Rectangle (width - .5 * (width - scale*next.Width), 0, .5 * (width - scale*next.Width), height);
				cr.Fill ();

				cr.Rectangle (0, 0, width, height);
				cr.Scale (scale, scale);
				CairoHelper.SetSourcePixbuf (cr, next, .5 * ((double)width/scale - next.Width), .5 * ((double)height/scale - next.Height));
				cr.PaintWithAlpha (progress);
				cr.Restore ();
			}
		}
	}
}
