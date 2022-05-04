//
// Choreographer.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Widgets
{
	public enum Blocking
	{
		Upstage,
		Downstage
	}
}

namespace Hyena.Gui.Theatrics
{
	public enum Easing
	{
		Linear,
		QuadraticIn,
		QuadraticOut,
		QuadraticInOut,
		ExponentialIn,
		ExponentialOut,
		ExponentialInOut,
		Sine,
	}

	public static class Choreographer
	{
		public static int PixelCompose (double percent, int size, Easing easing)
		{
			return (int)Math.Round (Compose (percent, size, easing));
		}

		public static double Compose (double percent, double scale, Easing easing)
		{
			return scale * Compose (percent, easing);
		}

		public static double Compose (double percent, Easing easing)
		{
			if (percent < 0.0 || percent > 1.0) {
				throw new ArgumentOutOfRangeException (nameof (percent), "must be between 0 and 1 inclusive");
			}

			switch (easing) {
			case Easing.QuadraticIn:
				return percent * percent;

			case Easing.QuadraticOut:
				return -1.0 * percent * (percent - 2.0);

			case Easing.QuadraticInOut:
				percent *= 2.0;
				return percent < 1.0
					? percent * percent * 0.5
					: -0.5 * (--percent * (percent - 2.0) - 1.0);

			case Easing.ExponentialIn:
				return Math.Pow (2.0, 10.0 * (percent - 1.0));

			case Easing.ExponentialOut:
				return -Math.Pow (2.0, -10.0 * percent) + 1.0;

			case Easing.ExponentialInOut:
				percent *= 2.0;
				return percent < 1.0
					? 0.5 * Math.Pow (2.0, 10.0 * (percent - 1.0))
					: 0.5 * (-Math.Pow (2.0, -10.0 * --percent) + 2.0);

			case Easing.Sine:
				return Math.Sin (percent * Math.PI);

			case Easing.Linear:
			default:
				return percent;
			}
		}
	}
}