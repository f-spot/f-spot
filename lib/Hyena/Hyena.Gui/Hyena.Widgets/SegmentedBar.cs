//
// SegmentedBar.cs
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
using System.Collections.Generic;

using Gtk;
using Cairo;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public class SegmentedBar : Widget
    {
        public delegate string BarValueFormatHandler (Segment segment);

        public class Segment
        {
            private string title;
            private double percent;
            private Cairo.Color color;
            private bool show_in_bar;

            public Segment (string title, double percent, Cairo.Color color)
                : this (title, percent, color, true)
            {
            }

            public Segment (string title, double percent, Cairo.Color color, bool showInBar)
            {
                this.title = title;
                this.percent = percent;
                this.color = color;
                this.show_in_bar = showInBar;
            }

            public string Title {
                get { return title; }
                set { title = value; }
            }

            public double Percent {
                get { return percent; }
                set { percent = value; }
            }

            public Cairo.Color Color {
                get { return color; }
                set { color = value; }
            }

            public bool ShowInBar {
                get { return show_in_bar; }
                set { show_in_bar = value; }
            }

            internal int LayoutWidth;
            internal int LayoutHeight;
        }

        // State
        private List<Segment> segments = new List<Segment> ();
        private int layout_width;
        private int layout_height;

        // Properties
        private int bar_height = 26;
        private int bar_label_spacing = 8;
        private int segment_label_spacing = 16;
        private int segment_box_size = 12;
        private int segment_box_spacing = 6;
        private int h_padding = 0;

        private bool show_labels = true;
        private bool reflect = true;

        private Color remainder_color = CairoExtensions.RgbToColor (0xeeeeee);

        private BarValueFormatHandler format_handler;

        public SegmentedBar ()
        {
            WidgetFlags |= WidgetFlags.NoWindow;
        }

        protected override void OnRealized ()
        {
            GdkWindow = Parent.GdkWindow;
            base.OnRealized ();
        }

#region Size Calculations

        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition.Width = 200;
            requisition.Height = 0;
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            int _bar_height = reflect ? (int)Math.Ceiling (bar_height * 1.75) : bar_height;

            if (show_labels) {
                ComputeLayoutSize ();
                HeightRequest = Math.Max (bar_height + bar_label_spacing + layout_height, _bar_height);
                WidthRequest = layout_width + (2 * h_padding);
            } else {
                HeightRequest = _bar_height;
                WidthRequest = bar_height + (2 * h_padding);
            }

            base.OnSizeAllocated (allocation);
        }

        private void ComputeLayoutSize ()
        {
            if (segments.Count == 0) {
                return;
            }

            Pango.Layout layout = null;

            layout_width = layout_height = 0;

            for (int i = 0, n = segments.Count; i < n; i++) {
                int aw, ah, bw, bh;

                layout = CreateAdaptLayout (layout, false, true);
                layout.SetText (FormatSegmentText (segments[i]));
                layout.GetPixelSize (out aw, out ah);

                layout = CreateAdaptLayout (layout, true, false);
                layout.SetText (FormatSegmentValue (segments[i]));
                layout.GetPixelSize (out bw, out bh);

                int w = Math.Max (aw, bw);
                int h = ah + bh;

                segments[i].LayoutWidth = w;
                segments[i].LayoutHeight = Math.Max (h, segment_box_size * 2);

                layout_width += segments[i].LayoutWidth + segment_box_size + segment_box_spacing
                    + (i < n - 1 ? segment_label_spacing : 0);
                layout_height = Math.Max (layout_height, segments[i].LayoutHeight);
            }

            layout.Dispose ();
        }

#endregion

#region Public Methods

        public void AddSegmentRgba (string title, double percent, uint rgbaColor)
        {
            AddSegment (title, percent, CairoExtensions.RgbaToColor (rgbaColor));
        }

        public void AddSegmentRgb (string title, double percent, uint rgbColor)
        {
            AddSegment (title, percent, CairoExtensions.RgbToColor (rgbColor));
        }

        public void AddSegment (string title, double percent, Color color)
        {
            AddSegment (new Segment (title, percent, color, true));
        }

        public void AddSegment (string title, double percent, Color color, bool showInBar)
        {
            AddSegment (new Segment (title, percent, color, showInBar));
        }

        public void AddSegment (Segment segment)
        {
            lock (segments) {
                segments.Add (segment);
                QueueDraw ();
            }
        }

        public void UpdateSegment (int index, double percent)
        {
            segments[index].Percent = percent;
            QueueDraw ();
        }

#endregion

#region Public Properties

        public BarValueFormatHandler ValueFormatter {
            get { return format_handler; }
            set { format_handler = value; }
        }

        public Color RemainderColor {
            get { return remainder_color; }
            set {
                remainder_color = value;
                QueueDraw ();
            }
        }

        public int BarHeight {
            get { return bar_height; }
            set {
                if (bar_height != value) {
                    bar_height = value;
                    QueueResize ();
                }
            }
        }

        public bool ShowReflection {
            get { return reflect; }
            set {
                if (reflect != value) {
                    reflect = value;
                    QueueResize ();
                }
            }
        }

        public bool ShowLabels {
            get { return show_labels; }
            set {
                if (show_labels != value) {
                    show_labels = value;
                    QueueResize ();
                }
            }
        }

        public int SegmentLabelSpacing {
            get { return segment_label_spacing; }
            set {
                if (segment_label_spacing != value) {
                    segment_label_spacing = value;
                    QueueResize ();
                }
            }
        }
        public int SegmentBoxSize {
            get { return segment_box_size; }
            set {
                if (segment_box_size != value) {
                    segment_box_size = value;
                    QueueResize ();
                }
            }
        }

        public int SegmentBoxSpacing {
            get { return segment_box_spacing; }
            set {
                if (segment_box_spacing != value) {
                    segment_box_spacing = value;
                    QueueResize ();
                }
            }
        }

        public int BarLabelSpacing {
            get { return bar_label_spacing; }
            set {
                if (bar_label_spacing != value) {
                    bar_label_spacing = value;
                    QueueResize ();
                }
            }
        }

        public int HorizontalPadding {
            get { return h_padding; }
            set {
                if (h_padding != value) {
                    h_padding = value;
                    QueueResize ();
                }
            }
        }

#endregion

#region Rendering

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (evnt.Window != GdkWindow) {
                return base.OnExposeEvent (evnt);
            }

            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);

            if (reflect) {
                CairoExtensions.PushGroup (cr);
            }

            cr.Operator = Operator.Over;
            cr.Translate (Allocation.X + h_padding, Allocation.Y);
            cr.Rectangle (0, 0, Allocation.Width - h_padding, Math.Max (2 * bar_height,
                bar_height + bar_label_spacing + layout_height));
            cr.Clip ();

            Pattern bar = RenderBar (Allocation.Width - 2 * h_padding, bar_height);

            cr.Save ();
            cr.Source = bar;
            cr.Paint ();
            cr.Restore ();

            if (reflect) {
                cr.Save ();

                cr.Rectangle (0, bar_height, Allocation.Width - h_padding, bar_height);
                cr.Clip ();

                Matrix matrix = new Matrix ();
                matrix.InitScale (1, -1);
                matrix.Translate (0, -(2 * bar_height) + 1);
                cr.Transform (matrix);

                cr.Pattern = bar;

                LinearGradient mask = new LinearGradient (0, 0, 0, bar_height);

                mask.AddColorStop (0.25, new Color (0, 0, 0, 0));
                mask.AddColorStop (0.5, new Color (0, 0, 0, 0.125));
                mask.AddColorStop (0.75, new Color (0, 0, 0, 0.4));
                mask.AddColorStop (1.0, new Color (0, 0, 0, 0.7));

                cr.Mask (mask);
                mask.Destroy ();

                cr.Restore ();

                CairoExtensions.PopGroupToSource (cr);
                cr.Paint ();
            }

            if (show_labels) {
                cr.Translate ((reflect ? Allocation.X : -h_padding) + (Allocation.Width - layout_width) / 2,
                     (reflect ? Allocation.Y : 0) + bar_height + bar_label_spacing);

                RenderLabels (cr);
            }

            bar.Destroy ();
            CairoExtensions.DisposeContext (cr);

            return true;
        }

        private Pattern RenderBar (int w, int h)
        {
            ImageSurface s = new ImageSurface (Format.Argb32, w, h);
            Context cr = new Context (s);
            RenderBar (cr, w, h, h / 2);
// TODO Implement the new ctor - see http://bugzilla.gnome.org/show_bug.cgi?id=561394
#pragma warning disable 0618
            Pattern pattern = new Pattern (s);
#pragma warning restore 0618
            s.Destroy ();
            ((IDisposable)cr).Dispose ();
            return pattern;
        }

        private void RenderBar (Context cr, int w, int h, int r)
        {
            RenderBarSegments (cr, w, h, r);
            RenderBarStrokes (cr, w, h, r);
        }

        private void RenderBarSegments (Context cr, int w, int h, int r)
        {
            LinearGradient grad = new LinearGradient (0, 0, w, 0);
            double last = 0.0;

            foreach (Segment segment in segments) {
                if (segment.Percent > 0) {
                    grad.AddColorStop (last, segment.Color);
                    grad.AddColorStop (last += segment.Percent, segment.Color);
                }
            }

            CairoExtensions.RoundedRectangle (cr, 0, 0, w, h, r);
            cr.Pattern = grad;
            cr.FillPreserve ();
            cr.Pattern.Destroy ();

            grad = new LinearGradient (0, 0, 0, h);
            grad.AddColorStop (0.0, new Color (1, 1, 1, 0.125));
            grad.AddColorStop (0.35, new Color (1, 1, 1, 0.255));
            grad.AddColorStop (1, new Color (0, 0, 0, 0.4));

            cr.Pattern = grad;
            cr.Fill ();
            cr.Pattern.Destroy ();
        }

        private void RenderBarStrokes (Context cr, int w, int h, int r)
        {
            LinearGradient stroke = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0x00000040));
            LinearGradient seg_sep_light = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0xffffff20));
            LinearGradient seg_sep_dark = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0x00000020));

            cr.LineWidth = 1;

            double seg_w = 20;
            double x = seg_w > r ? seg_w : r;

            while (x <= w - r) {
                cr.MoveTo (x - 0.5, 1);
                cr.LineTo (x - 0.5, h - 1);
                cr.Pattern = seg_sep_light;
                cr.Stroke ();

                cr.MoveTo (x + 0.5, 1);
                cr.LineTo (x + 0.5, h - 1);
                cr.Pattern = seg_sep_dark;
                cr.Stroke ();

                x += seg_w;
            }

            CairoExtensions.RoundedRectangle (cr, 0.5, 0.5, w - 1, h - 1, r);
            cr.Pattern = stroke;
            cr.Stroke ();

            stroke.Destroy ();
            seg_sep_light.Destroy ();
            seg_sep_dark.Destroy ();
        }

        private LinearGradient MakeSegmentGradient (int h, Color color)
        {
            return MakeSegmentGradient (h, color, false);
        }

        private LinearGradient MakeSegmentGradient (int h, Color color, bool diag)
        {
            LinearGradient grad = new LinearGradient (0, 0, 0, h);
            grad.AddColorStop (0, CairoExtensions.ColorShade (color, 1.1));
            grad.AddColorStop (0.35, CairoExtensions.ColorShade (color, 1.2));
            grad.AddColorStop (1, CairoExtensions.ColorShade (color, 0.8));
            return grad;
        }

        private void RenderLabels (Context cr)
        {
            if (segments.Count == 0) {
                return;
            }

            Pango.Layout layout = null;
            Color text_color = CairoExtensions.GdkColorToCairoColor (Style.Foreground (State));
            Color box_stroke_color = new Color (0, 0, 0, 0.6);

            int x = 0;

            foreach (Segment segment in segments) {
                cr.LineWidth = 1;
                cr.Rectangle (x + 0.5, 2 + 0.5, segment_box_size - 1, segment_box_size - 1);
                LinearGradient grad = MakeSegmentGradient (segment_box_size, segment.Color, true);
                cr.Pattern = grad;
                cr.FillPreserve ();
                cr.Color = box_stroke_color;
                cr.Stroke ();
                grad.Destroy ();

                x += segment_box_size + segment_box_spacing;

                int lw, lh;
                layout = CreateAdaptLayout (layout, false, true);
                layout.SetText (FormatSegmentText (segment));
                layout.GetPixelSize (out lw, out lh);

                cr.MoveTo (x, 0);
                text_color.A = 0.9;
                cr.Color = text_color;
                PangoCairoHelper.ShowLayout (cr, layout);
                cr.Fill ();

                layout = CreateAdaptLayout (layout, true, false);
                layout.SetText (FormatSegmentValue (segment));

                cr.MoveTo (x, lh);
                text_color.A = 0.75;
                cr.Color = text_color;
                PangoCairoHelper.ShowLayout (cr, layout);
                cr.Fill ();

                x += segment.LayoutWidth + segment_label_spacing;
            }

            layout.Dispose ();
        }

#endregion

#region Utilities

        private int pango_size_normal;

        private Pango.Layout CreateAdaptLayout (Pango.Layout layout, bool small, bool bold)
        {
            if (layout == null) {
                Pango.Context context = CreatePangoContext ();
                layout = new Pango.Layout (context);
                layout.FontDescription = context.FontDescription;
                pango_size_normal = layout.FontDescription.Size;
            }

            layout.FontDescription.Size = small
                ? (int)(layout.FontDescription.Size * Pango.Scale.Small)
                : pango_size_normal;

            layout.FontDescription.Weight = bold
                ? Pango.Weight.Bold
                : Pango.Weight.Normal;

            return layout;
        }


        private string FormatSegmentText (Segment segment)
        {
            return segment.Title;
        }

        private string FormatSegmentValue (Segment segment)
        {
            return format_handler == null
                ? String.Format ("{0}%", segment.Percent * 100.0)
                : format_handler (segment);
        }

#endregion

    }

#region Test Module

    [TestModule ("Segmented Bar")]
    internal class SegmentedBarTestModule : Window
    {
        private SegmentedBar bar;
        private VBox box;
        public SegmentedBarTestModule () : base ("Segmented Bar")
        {
            BorderWidth = 10;
            AppPaintable = true;

            box = new VBox ();
            box.Spacing = 10;
            Add (box);

            int space = 55;
            bar = new SegmentedBar ();
            bar.HorizontalPadding = bar.BarHeight / 2;
            bar.AddSegmentRgb ("Audio", 0.00187992456702332, 0x3465a4);
            bar.AddSegmentRgb ("Other", 0.0788718162651326, 0xf57900);
            bar.AddSegmentRgb ("Video", 0.0516869922033282, 0x73d216);
            bar.AddSegment ("Free Space", 0.867561266964516, bar.RemainderColor, false);

            bar.ValueFormatter = delegate (SegmentedBar.Segment segment) {
                return String.Format ("{0} GB", space * segment.Percent);
            };

            HBox controls = new HBox ();
            controls.Spacing = 5;

            Label label = new Label ("Height:");
            controls.PackStart (label, false, false, 0);

            SpinButton height = new SpinButton (new Adjustment (bar.BarHeight, 5, 100, 1, 1, 1), 1, 0);
            height.Activated += delegate { bar.BarHeight = height.ValueAsInt; };
            height.Changed += delegate { bar.BarHeight = height.ValueAsInt; bar.HorizontalPadding = bar.BarHeight / 2; };
            controls.PackStart (height, false, false, 0);

            CheckButton reflect = new CheckButton ("Reflection");
            reflect.Active = bar.ShowReflection;
            reflect.Toggled += delegate { bar.ShowReflection = reflect.Active; };
            controls.PackStart (reflect, false, false, 0);

            CheckButton labels = new CheckButton ("Labels");
            labels.Active = bar.ShowLabels;
            labels.Toggled += delegate { bar.ShowLabels = labels.Active; };
            controls.PackStart (labels, false, false, 0);

            box.PackStart (controls, false, false, 0);
            box.PackStart (new HSeparator (), false, false, 0);
            box.PackStart (bar, false, false, 0);
            box.ShowAll ();

            SetSizeRequest (350, -1);

            Gdk.Geometry limits = new Gdk.Geometry ();
            limits.MinWidth = SizeRequest ().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            SetGeometryHints (this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }
    }

#endregion

}
