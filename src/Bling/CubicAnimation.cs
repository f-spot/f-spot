//
// FSpot.Bling.CubicAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public abstract class CubicAnimation<T>: EasedAnimation<T>
	{
		public CubicAnimation (T from, T to, TimeSpan duration, Action<T> action) : base (from, to, duration, action)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
