//
// FSpot.Bling.BackAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public abstract class BackAnimation<T>: EasedAnimation<T>
	{
		double amplitude;

		public BackAnimation (T from, T to, TimeSpan duration, Action<T> action) : this (from, to, duration, action, 0)
		{
		}

		public BackAnimation (T from, T to, TimeSpan duration, Action<T> action, double amplitude) : base (from, to, duration, action)
		{
			if (amplitude < 0)
				throw new ArgumentOutOfRangeException ("amplitude");
			this.amplitude = amplitude;
		}

		public double Amplitude {
			get { return amplitude; }
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException ("amplitude");
				amplitude = value;
			}
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime - normalizedTime * amplitude * Math.Sin (normalizedTime * Math.PI);
		}
	}
}
