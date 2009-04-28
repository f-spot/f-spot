//
// FSpot.Bling.BackEase.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public abstract class BackEase : EasingFunction
	{
		double amplitude;

		public BackEase () : this (1.0)
		{
		}

		public BackEase (double amplitude) : base ()
		{
			if (amplitude < 0)
				throw new ArgumentOutOfRangeException ("amplitude");
			this.amplitude = amplitude;
		}

		public BackEase (EasingMode easingMode) : this (easingMode, 1.0)
		{
		}

		public BackEase (EasingMode easingMode, double amplitude) : base (easingMode)
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
