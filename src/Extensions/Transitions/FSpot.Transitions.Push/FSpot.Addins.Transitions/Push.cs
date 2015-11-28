//
// PushTransition.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
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

using System;

using Mono.Unix;

using Cairo;
using Gdk;

using FSpot.Transitions;

using Color = Cairo.Color;

namespace FSpot.Addins.Transitions
{
	public class Push : CairoTransition
	{
		public Push () : base (Catalog.GetString ("Push"))
		{
		}

		protected override void Draw (Context cr, Pixbuf prev, Pixbuf next, int width, int height, double progress)
		{
			cr.SetSourceColor (new Color (0, 0, 0));
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
