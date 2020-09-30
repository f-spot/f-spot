//
// BackEase.cs
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
	public abstract class BackEase : EasingFunction
	{
		double amplitude;

		protected BackEase () : this (1.0)
		{
		}

		protected BackEase (double amplitude)
		{
			if (amplitude < 0)
				throw new ArgumentOutOfRangeException (nameof (amplitude));
			this.amplitude = amplitude;
		}

		protected BackEase (EasingMode easingMode) : this (easingMode, 1.0)
		{
		}

		protected BackEase (EasingMode easingMode, double amplitude) : base (easingMode)
		{
			if (amplitude < 0)
				throw new ArgumentOutOfRangeException (nameof (amplitude));
			this.amplitude = amplitude;
		}

		public double Amplitude {
			get => amplitude;
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException (nameof (amplitude));
				amplitude = value;
			}
		}

		protected override double EaseInCore (double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime - normalizedTime * amplitude * Math.Sin (normalizedTime * Math.PI);
		}
	}
}
