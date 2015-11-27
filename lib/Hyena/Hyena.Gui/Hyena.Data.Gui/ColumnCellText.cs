//
// ColumnCellText.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Canvas;
using Hyena.Gui.Theming;
using Hyena.Data.Gui.Accessibility;

namespace Hyena.Data.Gui
{
    public class ColumnCellText : ColumnCell, ISizeRequestCell, ITextCell, ITooltipCell
    {
        public delegate string DataHandler ();

        private Pango.Weight font_weight = Pango.Weight.Normal;
        private Pango.EllipsizeMode ellipsize_mode = Pango.EllipsizeMode.End;
        private Pango.Alignment alignment = Pango.Alignment.Left;
        private int text_width;
        private int text_height;
        private string text_format = null;
        protected string MinString, MaxString;
        private string last_text = null;
        private bool use_markup;

        public ColumnCellText (string property, bool expand) : base (property, expand)
        {
            Padding = new Thickness (4, 0);
            SingleParagraphMode = true;
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new ColumnCellTextAccessible (BoundObject, this, parent);
        }

        public override string GetTextAlternative (object obj)
        {
            return GetText (obj);
        }

        public void SetMinMaxStrings (object min_max)
        {
            SetMinMaxStrings (min_max, min_max);
        }

        public void SetMinMaxStrings (object min, object max)
        {
            // Set the min/max strings from the min/max objects
            MinString = GetText (min);
            MaxString = GetText (max);
            RestrictSize = true;
        }

        //int? height;
        //public override Size Measure (Size available)
        //{
            //int min, max;
            //GetWidthRange (ParentLayout.View.PangoLayout, out min, out max);
            //
            /*if (height == null) {
                using (var layout = new Pango.Layout (ParentLayout.View.PangoContext)) {
                    if (layout.FontDescription == null) {
                        layout.FontDescription = new Pango.FontDescription ();
                    }
                    UpdateText (layout, 100, "Woo Mar");
                    height = TextHeight;
                }
            }*/

            //return FixedSize ?? new Size (0, (double)height + Padding.Y);
        //}

        public override void Render (CellContext context, double cellWidth, double cellHeight)
        {
            UpdateText (context, cellWidth);
            if (String.IsNullOrEmpty (last_text)) {
                return;
            }

            //context.Context.Rectangle (0, 0, cellWidth, cellHeight);
            //context.Context.Clip ();
            context.Context.MoveTo (Padding.Left, ((int)cellHeight - text_height) / 2);
            Cairo.Color color = context.Theme.Colors.GetWidgetColor (
                context.TextAsForeground ? GtkColorClass.Foreground : GtkColorClass.Text, context.State);
            color.A = Alpha ?? (context.Opaque ? 1.0 : 0.5);
            context.Context.Color = color;

            PangoCairoHelper.ShowLayout (context.Context, context.Layout);
            //context.Context.ResetClip ();
        }

        public void UpdateText (CellContext context, double cellWidth)
        {
            string text = last_text = GetText (BoundObject);
            UpdateText (context.Layout, cellWidth, text);
        }

        public void UpdateText (Pango.Layout layout, double cellWidth, string text)
        {
            if (String.IsNullOrEmpty (text)) {
                return;
            }

            layout.Width = (int)((cellWidth - Padding.X) * Pango.Scale.PangoScale);
            layout.FontDescription.Weight = font_weight;
            layout.Ellipsize = EllipsizeMode;
            layout.Alignment = alignment;
            layout.SingleParagraphMode = SingleParagraphMode;
            UpdateLayout (layout, text);
            layout.GetPixelSize (out text_width, out text_height);
            is_ellipsized = layout.IsEllipsized;
        }

        private static char[] lfcr = new char[] {'\n', '\r'};
        private void UpdateLayout (Pango.Layout layout, string text)
        {
            string final_text = GetFormattedText (text);
            if (SingleParagraphMode && final_text.IndexOfAny (lfcr) >= 0) {
                final_text = final_text.Replace ("\r\n", "\x20").Replace ('\n', '\x20').Replace ('\r', '\x20');
            }
            if (use_markup) {
                layout.SetMarkup (final_text);
            } else {
                layout.SetText (final_text);
            }
        }

        public string GetTooltipMarkup (CellContext cellContext, double columnWidth)
        {
            UpdateText (cellContext, columnWidth);
            return IsEllipsized ? GLib.Markup.EscapeText (Text) : null;
        }

        public Func<object, string> TextGenerator { get; set; }

        protected virtual string GetText (object obj)
        {
            if (TextGenerator != null) {
                return TextGenerator (obj);
            } else {
                return obj == null ? String.Empty : obj.ToString ();
            }
        }

        private string GetFormattedText (string text)
        {
            if (text_format == null) {
                return text;
            }
            return String.Format (text_format, UseMarkup ? GLib.Markup.EscapeText (text) : text);
        }

        private bool is_ellipsized = false;
        public bool IsEllipsized {
            get { return is_ellipsized; }
        }

        public string Text {
            get { return last_text; }
        }

        protected int TextWidth {
            get { return text_width; }
        }

        protected int TextHeight {
            get { return text_height; }
        }

        public string TextFormat {
            get { return text_format; }
            set { text_format = value; }
        }

        public Pango.Alignment Alignment {
            get { return alignment; }
            set { alignment = value; }
        }

        public virtual Pango.Weight FontWeight {
            get { return font_weight; }
            set { font_weight = value; }
        }

        public bool SingleParagraphMode { get; set; }

        public virtual Pango.EllipsizeMode EllipsizeMode {
            get { return ellipsize_mode; }
            set { ellipsize_mode = value; }
        }

        internal static int ComputeRowHeight (Widget widget)
        {
            int w_width, row_height;
            Pango.Layout layout = new Pango.Layout (widget.PangoContext);
            layout.SetText ("W");
            layout.GetPixelSize (out w_width, out row_height);
            layout.Dispose ();
            return row_height + 8;
        }

        #region ISizeRequestCell implementation

        public void GetWidthRange (Pango.Layout layout, out int min, out int max)
        {
            int height;
            min = max = -1;

            if (!String.IsNullOrEmpty (MinString)) {
                UpdateLayout (layout, MinString);
                layout.GetPixelSize (out min, out height);
                min += (int)Padding.X;
                //Console.WriteLine ("for {0} got min {1} for {2}", this, min, MinString);
            }

            if (!String.IsNullOrEmpty (MaxString)) {
                UpdateLayout (layout, MaxString);
                layout.GetPixelSize (out max, out height);
                max += (int)Padding.X;
                //Console.WriteLine ("for {0} got max {1} for {2}", this, max, MaxString);
            }
        }

        private bool restrict_size = false;
        public bool RestrictSize {
            get { return restrict_size; }
            set { restrict_size = value; }
        }

        public bool UseMarkup {
            get { return use_markup; }
            set { use_markup = value; }
        }

        public double? Alpha { get; set; }

        #endregion
    }
}
