//
// Cover.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Unix;

using Cairo;
using Gdk;

using FSpot.Transitions;
using FSpot.Utils;

using Color = Cairo.Color;

namespace FSpot.Addins.Transitions
{
	public class Cover : CairoTransition
	{
		public Cover () : base (Catalog.GetString ("Cover"))
		{
		}

		protected override void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			cr.SetSourceColor (new Color (0, 0, 0));
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
				cr.Paint ();
				cr.Restore ();
			}
		}
	}
}
