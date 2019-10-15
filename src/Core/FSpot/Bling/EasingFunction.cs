//
// EasingFunction.cs
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
