//
// FSpot.Widgets.CairoTransition.cs
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
	public abstract class CairoTransition : SlideShowTransition
	{
		public CairoTransition (string name) : base (name)
		{
		}

		public override void Draw (Drawable d, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (d)) {
				Draw (cr, prev, next, width, height, progress);
			}
		}

		protected abstract void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress);
	}
}
