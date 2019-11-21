//
// Actor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Hyena.Gui.Theatrics
{
    public class Actor<T>
    {
        private DateTime start_time;
        private DateTime last_step_time;

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

        private static double PClamp (double value)
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
