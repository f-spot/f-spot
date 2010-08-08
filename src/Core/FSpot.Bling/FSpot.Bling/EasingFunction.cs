//
// FSpot.Bling.EasingFunction.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{
	public abstract class EasingFunction
	{
		EasingMode easingMode;

		public EasingFunction () : this (EasingMode.EaseIn)
		{
		}

		public EasingFunction (EasingMode easingMode)
		{
			this.easingMode = easingMode;
		}

		public double Ease (double normalizedTime)
		{
			switch (easingMode) {
			case EasingMode.EaseIn:
				return EaseInCore (normalizedTime);
			case EasingMode.EaseOut:
				return 1.0 - EaseInCore (1 - normalizedTime);
			case EasingMode.EaseInOut:
				return (normalizedTime <= 0.5
					? EaseInCore (normalizedTime * 2) * 0.5
					: 1.0 - EaseInCore ((1 - normalizedTime) * 2) * 0.5);
			}
			throw new InvalidOperationException ("Unknown value for EasingMode");
		}

		public EasingMode EasingMode {
			get { return easingMode; }
			set { easingMode = value; }
		}

		protected abstract double EaseInCore (double normalizedTime);
	}
}
