//
// FSpot.Bling.DoubleAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public class DoubleAnimation : EasedAnimation<double>
	{
		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action) : base (from, to, duration, action)
		{
		}

		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action, EasingFunction easingFunction) : base (from, to, duration, action, easingFunction)
		{
		}

		protected override double Interpolate (double from, double to, double progress)
		{
			return from + progress * (to - from);
		}
	}
}

