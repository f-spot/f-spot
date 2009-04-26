//
// FSpot.Bling.DoubleBackAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public class DoubleBackAnimation : BackAnimation<double>
	{
		public DoubleBackAnimation (double from, double to, TimeSpan duration, Action<double> action, double amplitude) : base (from, to, duration, action, amplitude)
		{
		}

		public DoubleBackAnimation (double from, double to, TimeSpan duration, Action<double> action) : this (from, to, duration, action, 0)
		{
		}

		protected override double Interpolate (double from, double to, double progress)
		{
			return from + progress * (to - from);
		}
	}
}

