//
// AnimatedVBox.cs
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
	public class AnimatedVBox : AnimatedBox
	{
		public AnimatedVBox () : base (false)
		{
		}

		protected AnimatedVBox (IntPtr raw) : base (raw)
		{
		}
	}
}