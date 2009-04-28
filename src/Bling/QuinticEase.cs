//
// FSpot.Bling.QuinticEase.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public abstract class QuinticEase : EasingFunction
	{
		public QuinticEase () : base ()
		{
		}
		
		public QuinticEase (EasingMode easingMode) : base (easingMode)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
