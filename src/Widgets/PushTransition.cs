//
// FSpot.Widgets.PushTransition.cs
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

using Color = Cairo.Color;

namespace FSpot.Widgets
{
	public class PushTransition : CairoTransition
	{
		public PushTransition () : base ("Push")
		{
		}

		protected override void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			cr.Color = new Color (0, 0, 0);
			if (prev != null) {
				double scale = Math.Min ((double)width/(double)prev.Width, (double)height/(double)prev.Height);
				cr.Save ();
				cr.Translate (-progress * width, 0);

				cr.Rectangle (0, 0, width, .5 * (height - scale*prev.Height));
				cr.Fill ();

				cr.Rectangle (0, height - .5 * (height - scale*prev.Height), width, .5 * (height - scale*prev.Height));
				cr.Fill ();

				cr.Rectangle (0, 0, .5 * (width - scale*prev.Width), height);
				cr.Fill ();

				cr.Rectangle (width - .5 * (width - scale*prev.Width), 0, .5 * (width - scale*prev.Width), height);
				cr.Fill ();

				cr.Rectangle (0, 0, width, height);
				cr.Scale (scale, scale);
				CairoHelper.SetSourcePixbuf (cr, prev, .5 * ((double)width/scale - prev.Width), .5 * ((double)height/scale - prev.Height));
				cr.Fill ();
				cr.Restore ();
			}
			if (next != null) {
				double scale = Math.Min ((double)width/(double)next.Width, (double)height/(double)next.Height);
				cr.Save ();
				cr.Translate (width * (1.0 - progress), 0);

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
				cr.Fill ();
				cr.Restore ();
			}
		}
	}
}
