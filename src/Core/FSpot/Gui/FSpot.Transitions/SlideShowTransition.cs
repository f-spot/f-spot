//
// SlideShowTransition.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gdk;

namespace FSpot.Transitions
{
	public abstract class SlideShowTransition
	{
		public string Name { get; }

		protected SlideShowTransition (string name)
		{
			Name = name;
		}

		public abstract void Draw (Drawable d, Pixbuf prev, Pixbuf next, int width, int height, double progress);
	}
}
