//
// Actor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Gui.Theatrics
{
	public class Actor<T>
	{
		DateTime start_time;
		DateTime last_step_time;

		public Actor (T target, double duration)
		{
			Target = target;
			Duration = duration;
			CanExpire = true;
			Reset ();
		}

		public void Reset ()
		{
			Reset (Duration);
		}

		public void Reset (double duration)
		{
			start_time = DateTime.Now;
			last_step_time = DateTime.Now;
			Frames = 0.0;
			Percent = 0.0;
			Duration = duration;
		}

		public virtual void Step ()
		{
			if (!CanExpire && Percent >= 1.0) {
				Reset ();
			}

			StepDelta = (DateTime.Now - last_step_time).TotalMilliseconds;
			last_step_time = DateTime.Now;
			Percent = PClamp ((last_step_time - start_time).TotalMilliseconds / Duration);
			StepDeltaPercent = PClamp (StepDelta / Duration);
			Frames++;
		}

		static double PClamp (double value)
		{
			return Math.Max (0.1, Math.Min (1.0, value));
		}

		public bool CanExpire { get; set; }
		public T Target { get; private set; }
		public double Duration { get; private set; }
		public DateTime StartTime { get; private set; }
		public double StepDelta { get; private set; }
		public double StepDeltaPercent { get; private set; }
		public double Percent { get; private set; }
		public double Frames { get; private set; }

		public double FramesPerSecond {
			get { return Frames / ((double)Duration / 1000.0); }
		}

		public bool Expired {
			get { return CanExpire && Percent >= 1.0; }
		}
	}
}
