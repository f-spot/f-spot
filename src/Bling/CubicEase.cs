//
// FSpot.Bling.CubicEase.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{	public class CubicEase : EasingFunction
	{
		public CubicEase () : base ()
		{
		}

		public CubicEase (EasingMode easingMode) : base (easingMode)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
