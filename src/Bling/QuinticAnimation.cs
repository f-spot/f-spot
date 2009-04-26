//
// FSpot.Bling.QuinticAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public abstract class QuinticAnimation<T>: EasedAnimation<T>
	{
		public QuinticAnimation (T from, T to, TimeSpan duration, Action<T> action) : base (from, to, duration, action)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
