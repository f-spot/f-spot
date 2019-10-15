//
// EasedAnimation.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
