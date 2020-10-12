//
// QuinticEase.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Bling
{	
	public abstract class QuinticEase : EasingFunction
	{
		protected QuinticEase ()
		{
		}
		
		protected QuinticEase (EasingMode easingMode) : base (easingMode)
		{
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
