//
// FpsCalculator.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Gui.Canvas
{
	public class FpsCalculator
	{
		DateTime last_update;
		TimeSpan update_interval;
		int frame_count;
		double fps;

		public FpsCalculator ()
		{
			update_interval = TimeSpan.FromSeconds (0.5);
		}

		public bool Update ()
		{
			bool updated = false;
			DateTime current_time = DateTime.Now;
			frame_count++;

			if (current_time - last_update >= update_interval) {
				fps = (double)frame_count / (current_time - last_update).TotalSeconds;
				frame_count = 0;
				updated = true;
				last_update = current_time;
			}

			return updated;
		}

		public double FramesPerSecond {
			get { return fps; }
		}
	}
}
