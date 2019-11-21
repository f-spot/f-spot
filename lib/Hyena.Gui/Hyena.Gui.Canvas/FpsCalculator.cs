//
// FpsCalculator.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using Gdk;

using Hyena.Gui.Theming;

namespace Hyena.Gui.Canvas
{
    public class FpsCalculator
    {
        private DateTime last_update;
        private TimeSpan update_interval;
        private int frame_count;
        private double fps;

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
