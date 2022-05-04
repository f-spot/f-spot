//
// DoubleAnimation.cs
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
	public class DoubleAnimation : EasedAnimation<double>
	{
		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action) : base (from, to, duration, action)
		{
		}

		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action, GLib.Priority priority) : base (from, to, duration, action, priority)
		{
		}

		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action, EasingFunction easingFunction) : base (from, to, duration, action, easingFunction)
		{
		}

		public DoubleAnimation (double from, double to, TimeSpan duration, Action<double> action, EasingFunction easingFunction, GLib.Priority priority) : base (from, to, duration, action, easingFunction, priority)
		{
		}

		protected override double Interpolate (double from, double to, double progress)
		{
			return from + progress * (to - from);
		}
	}
}
