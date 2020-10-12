//
// EasingFunction.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Bling
{
	public abstract class EasingFunction
	{
		protected EasingFunction () : this (EasingMode.EaseIn)
		{
		}

		protected EasingFunction (EasingMode easingMode)
		{
			EasingMode = easingMode;
		}

		public double Ease (double normalizedTime)
		{
			switch (EasingMode) {
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

		public EasingMode EasingMode { get; set; }

		protected abstract double EaseInCore (double normalizedTime);
	}
}
