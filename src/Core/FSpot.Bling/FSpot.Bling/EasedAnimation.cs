//
// FSpot.Bling.EasedAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

using System;

namespace FSpot.Bling
{
	public abstract class EasedAnimation<T>: Animation<T>
	{
		EasingFunction easingFunction;

		public EasedAnimation () : this (null)
		{
		}

		public EasedAnimation (EasingFunction easingFunction) : base ()
		{
			this.easingFunction = easingFunction;
		}

		public EasedAnimation (T from, T to, TimeSpan duration, Action<T> action) : this (from, to, duration, action, null)
		{
		}

		public EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, GLib.Priority priority) : this (from, to, duration, action, null, priority)
		{
		}

		public EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, EasingFunction easingFunction) : base (from, to, duration, action)
		{
			this.easingFunction = easingFunction;
		}

		public EasedAnimation (T from, T to, TimeSpan duration, Action<T> action, EasingFunction easingFunction, GLib.Priority priority) : base (from, to, duration, action, priority)
		{
			this.easingFunction = easingFunction;
		}

		public EasingFunction EasingFunction {
			get { return easingFunction; }
			set { easingFunction = value; }
		}

		protected override double Ease (double normalizedTime)
		{
			if (easingFunction == null)
				return base.Ease (normalizedTime);
			return easingFunction.Ease (normalizedTime);
		}

	}
}
