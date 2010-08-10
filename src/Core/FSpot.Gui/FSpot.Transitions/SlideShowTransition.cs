//
// FSpot.Widgets.SlideShowTransition.cs
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

namespace FSpot.Transitions
{
	public abstract class SlideShowTransition
	{
		public SlideShowTransition (string name)
		{
			this.name = name;
		}

		string name;
		public string Name {
			get { return name; }
		}

		public abstract void Draw (Drawable d, Pixbuf prev, Pixbuf next, int width, int height, double progress);
	}
}
