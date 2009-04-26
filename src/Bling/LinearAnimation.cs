//
// FSpot.Bling.LinearAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{
	public abstract class LinearAnimation<T> : EasedAnimation<T>
	{
		public LinearAnimation (T from, T to, TimeSpan duration, Action<T> action) : base (from, to, duration, action)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime;
		}
	}
}
