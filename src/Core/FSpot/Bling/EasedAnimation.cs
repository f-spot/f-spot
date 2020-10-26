//
// EasedAnimation.cs
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
	public abstract class EasedAnimation<T>: Animation<T>
	{
		protected EasedAnimation () : this (null)
		{
		}

		protected EasedAnimation (EasingFunction easingFunction)
		{
			EasingFunction = easingFunction;
		}

		protected EasedAnimation (T from, T to, TimeSpan duration, Action<T> action) : this (from, to, duration, action, null)
		{
		}

		protected EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, GLib.Priority priority) : this (from, to, duration, action, null, priority)
		{
		}

		protected EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, EasingFunction easingFunction) : base (from, to, duration, action)
		{
			EasingFunction = easingFunction;
		}

		protected EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, EasingFunction easingFunction, GLib.Priority priority) : base (from, to, duration, action, priority)
		{
			EasingFunction = easingFunction;
		}

		public EasingFunction EasingFunction { get; set; }

		protected override double Ease (double normalizedTime)
		{
			if (EasingFunction == null)
				return base.Ease (normalizedTime);
			return EasingFunction.Ease (normalizedTime);
		}
	}
}
