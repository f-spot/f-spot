//
// PulsingButton.cs
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
using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theatrics;

namespace Hyena.Widgets
{
    public class PulsingButton : Button
    {
        private static Stage<PulsingButton> default_stage;
        public static Stage<PulsingButton> DefaultStage {
            get {
                if (default_stage == null) {
                    default_stage = new Stage<PulsingButton> ();
                    default_stage.DefaultActorDuration = 1250;
                }

                return default_stage;
            }
        }

        private Pulsator<PulsingButton> pulsator = new Pulsator<PulsingButton> ();

        public Stage<PulsingButton> Stage {
            get { return pulsator.Stage; }
            set { pulsator.Stage = value; }
        }

        public PulsingButton () : base ()
        {
            Setup ();
        }

        public PulsingButton (string stock_id) : base (stock_id)
        {
            Setup ();
        }

        public PulsingButton (Widget widget) : base (widget)
        {
            Setup ();
        }

        protected PulsingButton (IntPtr raw) : base (raw)
        {
            Setup ();
        }

        private void Setup ()
        {
            Stage = DefaultStage;
            pulsator.Target = this;
            pulsator.Pulse += delegate { QueueDraw (); };
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!pulsator.IsPulsing) {
                return base.OnExposeEvent (evnt);
            }

            Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow);

            double x = Allocation.X + Allocation.Width / 2;
            double y = Allocation.Y + Allocation.Height / 2;
            double r = Math.Min (Allocation.Width, Allocation.Height) / 2;
            double alpha = Choreographer.Compose (pulsator.Percent, Easing.Sine);

            Cairo.Color color = CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Selected));
            Cairo.RadialGradient fill = new Cairo.RadialGradient (x, y, 0, x, y, r);
            color.A = alpha;
            fill.AddColorStop (0, color);
            fill.AddColorStop (0.5, color);
            color.A = 0;
            fill.AddColorStop (1, color);

            cr.Arc (x, y, r, 0, 2 * Math.PI);
            cr.Pattern = fill;
            cr.Fill ();
            fill.Destroy ();

            CairoExtensions.DisposeContext (cr);
            return base.OnExposeEvent (evnt);
        }

        public void StartPulsing ()
        {
            if (IsMapped && Sensitive) {
                pulsator.StartPulsing ();
            }
        }

        public void StopPulsing ()
        {
            pulsator.StopPulsing ();
        }

        protected override void OnStateChanged (StateType previous_state)
        {
            base.OnStateChanged (previous_state);
            if (State == StateType.Insensitive) {
                StopPulsing ();
            }
        }
    }
}
