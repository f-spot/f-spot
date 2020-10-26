//
// CairoTransition.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Cairo;

using Gdk;

namespace FSpot.Transitions
{
	public abstract class CairoTransition : SlideShowTransition
	{
		protected CairoTransition (string name) : base (name)
		{
		}

		public override void Draw (Drawable d, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			using Context cr = CairoHelper.Create (d);
			Draw (cr, prev, next, width, height, progress);
		}

		protected abstract void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress);
	}
}
