//
// Slider.cs
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
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Gui.Canvas
{
    public class Slider : CanvasItem
    {
        private uint value_changed_inhibit_ref = 0;

        public event EventHandler<EventArgs> ValueChanged;
        public event EventHandler<EventArgs> PendingValueChanged;

        public Slider ()
        {
            Margin = new Thickness (3);
            MarginStyle = new ShadowMarginStyle {
                ShadowSize = 3,
                ShadowOpacity = 0.25
            };
        }

        protected virtual void OnValueChanged ()
        {
            if (value_changed_inhibit_ref != 0) {
                return;
            }

            var handler = ValueChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected virtual void OnPendingValueChanged ()
        {
            var handler = PendingValueChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void InhibitValueChangeEvent ()
        {
            value_changed_inhibit_ref++;
        }

        public void UninhibitValueChangeEvent ()
        {
            value_changed_inhibit_ref--;
        }

        private void SetPendingValueFromX (double x)
        {
            IsValueUpdatePending = true;
            PendingValue = Math.Max (0, Math.Min ((x - ThrobberSize / 2) / RenderSize.Width, 1));
        }

        public override bool ButtonEvent (Point cursor, bool pressed, uint button)
        {
            if (pressed && button == 1) {
                GrabPointer ();
                SetPendingValueFromX (cursor.X);
                return true;
            } else if (!pressed && IsPointerGrabbed) {
                ReleasePointer ();
                Value = PendingValue;
                IsValueUpdatePending = false;
                return true;
            }
            return false;
        }

        public override bool CursorMotionEvent (Point cursor)
        {
            if (IsPointerGrabbed) {
                SetPendingValueFromX (cursor.X);
                return true;
            }
            return false;
        }

        //private double last_invalidate_value = -1;

        /*private void Invalidate ()
        {
            double current_value = (IsValueUpdatePending ? PendingValue : Value);

            // FIXME: Something is wrong with the updating below causing an
            // invalid region when IsValueUpdatePending is true, so when
            // that is the case for now, we trigger a full invalidation
            if (last_invalidate_value < 0 || IsValueUpdatePending) {
                last_invalidate_value = current_value;
                InvalidateRender ();
                return;
            }

            double max = Math.Max (last_invalidate_value, current_value) * RenderSize.Width;
            double min = Math.Min (last_invalidate_value, current_value) * RenderSize.Width;

            Rect region = new Rect (
                InvalidationRect.X + min,
                InvalidationRect.Y,
                (max - min) + 2 * ThrobberSize,
                InvalidationRect.Height
            );

            last_invalidate_value = current_value;
            InvalidateRender (region);
        }*/

        /*protected override Rect InvalidationRect {
            get { return new Rect (
                -Margin.Left - ThrobberSize / 2,
                -Margin.Top,
                Allocation.Width + ThrobberSize,
                Allocation.Height);
            }
        }*/

        protected override void ClippedRender (Cairo.Context cr)
        {
            double throbber_r = ThrobberSize / 2.0;
            double throbber_x = Math.Round (RenderSize.Width * (IsValueUpdatePending ? PendingValue : Value));
            double throbber_y = (Allocation.Height - ThrobberSize) / 2.0 - Margin.Top + throbber_r;
            double bar_w = RenderSize.Width * Value;

            cr.Color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, Gtk.StateType.Normal);
            cr.Rectangle (0, 0, RenderSize.Width, RenderSize.Height);
            cr.Fill ();

            Color color = Theme.Colors.GetWidgetColor (GtkColorClass.Dark, Gtk.StateType.Active);
            Color fill_color = CairoExtensions.ColorShade (color, 0.4);
            Color light_fill_color = CairoExtensions.ColorShade (color, 0.3);
            fill_color.A = 1.0;
            light_fill_color.A = 1.0;

            LinearGradient fill = new LinearGradient (0, 0, 0, RenderSize.Height);
            fill.AddColorStop (0, light_fill_color);
            fill.AddColorStop (0.5, fill_color);
            fill.AddColorStop (1, light_fill_color);

            cr.Rectangle (0, 0, bar_w, RenderSize.Height);
            cr.Pattern = fill;
            cr.Fill ();

            cr.Color = fill_color;
            cr.Arc (throbber_x, throbber_y, throbber_r, 0, Math.PI * 2);
            cr.Fill ();
        }

        public override Size Measure (Size available)
        {
            Height = BarSize;
            return DesiredSize = new Size (base.Measure (available).Width,
                Height + Margin.Top + Margin.Bottom);
        }

        private double bar_size = 3;
        public virtual double BarSize {
            get { return bar_size; }
            set { bar_size = value; }
        }

        private double throbber_size = 7;
        public virtual double ThrobberSize {
            get { return throbber_size; }
            set { throbber_size = value; }
        }

        private double value;
        public virtual double Value {
            get { return this.value; }
            set {
                if (value < 0.0 || value > 1.0) {
                    throw new ArgumentOutOfRangeException ("Value", "Must be between 0.0 and 1.0 inclusive");
                } else if (this.value == value) {
                    return;
                }

                this.value = value;
                Invalidate ();
                OnValueChanged ();
            }
        }

        private bool is_value_update_pending;
        public virtual bool IsValueUpdatePending {
            get { return is_value_update_pending; }
            set { is_value_update_pending = value; }
        }

        private double pending_value;
        public virtual double PendingValue {
            get { return pending_value; }
            set {
                if (value < 0.0 || value > 1.0) {
                    throw new ArgumentOutOfRangeException ("Value", "Must be between 0.0 and 1.0 inclusive");
                } else if (pending_value == value) {
                    return;
                }

                pending_value = value;
                Invalidate ();
                OnPendingValueChanged ();
            }
        }
    }
}
