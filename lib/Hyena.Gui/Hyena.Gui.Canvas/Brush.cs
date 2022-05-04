//
// Brush.cs
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
	public class Brush
	{
		Cairo.Color color;

		public Brush ()
		{
		}

		public Brush (byte r, byte g, byte b) : this (r, g, b, 255)
		{
		}

		public Brush (byte r, byte g, byte b, byte a)
			: this ((double)r / 255.0, (double)g / 255.0, (double)b / 255.0, (double)a / 255.0)
		{
		}

		public Brush (double r, double g, double b) : this (r, g, b, 1)
		{
		}

		public Brush (double r, double g, double b, double a) : this (new Cairo.Color (r, g, b, a))
		{
		}

		public Brush (Cairo.Color color)
		{
			this.color = color;
		}

		public virtual bool IsValid {
			get { return true; }
		}

		public virtual void Apply (Cairo.Context cr)
		{
			cr.SetSourceColor (color);
		}

		public static readonly Brush Black = new Brush (0.0, 0.0, 0.0);
		public static readonly Brush White = new Brush (1.0, 1.0, 1.0);

		public virtual double Width {
			get { return double.NaN; }
		}

		public virtual double Height {
			get { return double.NaN; }
		}
	}
}
