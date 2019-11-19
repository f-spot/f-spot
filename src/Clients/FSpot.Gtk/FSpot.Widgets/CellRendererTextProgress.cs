//
// CellRendererTextProgress.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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

using Gtk;

namespace FSpot.Widgets
{
	/*
	 * Because subclassing of CellRendererText does not to work, we
	 * use a new cellrenderer, which renderes a simple text and a
	 * progress bar below the text similar to the one used in baobab (gnome-utils)
	 */
	public class CellRendererTextProgress : CellRenderer
	{
		readonly int progress_width;
		readonly int progress_height;

		static Gdk.Color green = new Gdk.Color (0xcc, 0x00, 0x00);
		static Gdk.Color yellow = new Gdk.Color (0xed, 0xd4, 0x00);
		static Gdk.Color red = new Gdk.Color (0x73, 0xd2, 0x16);

		public CellRendererTextProgress () : this (70, 8)
		{
		}

		public CellRendererTextProgress (int progress_width, int progress_height)
		{
			this.progress_width = progress_width;
			this.progress_height = progress_height;

			Xalign = 0.0f;
			Yalign = 0.5f;

			Xpad = Ypad = 2;
		}

		protected CellRendererTextProgress (IntPtr ptr) : base (ptr)
		{
		}

		int progress_value;

		[GLib.PropertyAttribute ("value")]
		public int Value {
			get { return progress_value; }
			set {
				/* normalize value */
				progress_value = Math.Max (Math.Min (value, 100), 0);
			}
		}

		Pango.Layout text_layout;
		string text;

		[GLib.PropertyAttribute ("text")]
		public string Text {
			get { return text; }
			set {
				if (text == value)
					return;

				text = value;
				text_layout = null;
			}
		}

		bool use_markup;
		public bool UseMarkup {
			get { return use_markup; }
			set {
				if (use_markup == value)
					return;

				use_markup = value;
				text_layout = null;
			}
		}

		void UpdateLayout (Widget widget)
		{
			text_layout = new Pango.Layout (widget.PangoContext);

			if (UseMarkup)
				text_layout.SetMarkup (text);
			else
				text_layout.SetText (text);
		}

		Gdk.Color GetValueColor ()
		{
			if (progress_value <= 33)
				return green;

			if (progress_value <= 66)
				return yellow;

			return red;
		}

		public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			if (text_layout == null)
				UpdateLayout (widget);

			int text_width, text_height;

			text_layout.GetPixelSize (out text_width, out text_height);

			width = (int) (2 * Xpad + Math.Max (progress_width, text_width));
			height = (int) (3 * Ypad + progress_height + text_height);

			x_offset = Math.Max ((int) (Xalign * (cell_area.Width - width)), 0);
			y_offset = Math.Max ((int) (Yalign * (cell_area.Height - height)), 0);
		}

		protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
		{
			base.Render (window, widget, background_area, cell_area, expose_area, flags);

			if (text_layout == null)
				UpdateLayout (widget);

			int x, y, width, height, text_width, text_height;

			/* first render the text */
			text_layout.GetPixelSize (out text_width, out text_height);

			x  = (int) (cell_area.X + Xpad + Math.Max ((int) (Xalign * (cell_area.Width - 2 * Xpad - text_width)), 0));
			y  = (int) (cell_area.Y + Ypad);

			Style.PaintLayout (widget.Style,
			                   window,
			                   StateType.Normal,
			                   true,
			                   cell_area,
			                   widget,
			                   "cellrenderertextprogress",
			                   x, y,
			                   text_layout);

			y += (int) (text_height + Ypad);
			x  = (int) (cell_area.X + Xpad + Math.Max ((int) (Xalign * (cell_area.Width - 2 * Xpad - progress_width)), 0));


			/* second render the progress bar */
			using (Cairo.Context cairo_context = Gdk.CairoHelper.Create (window)) {

				width = progress_width;
				height = progress_height;

				cairo_context.Rectangle (x, y, width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, widget.Style.Dark (StateType.Normal));
				cairo_context.Fill ();

				x += widget.Style.XThickness;
				y += widget.Style.XThickness;
				width -= 2* widget.Style.XThickness;
				height -= 2 * widget.Style.Ythickness;

				cairo_context.Rectangle (x, y, width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, widget.Style.Light (StateType.Normal));
				cairo_context.Fill ();

				/* scale the value and ensure, that at least one pixel is drawn, if the value is greater than zero */
				int scaled_width =
					(int) Math.Max (((progress_value * width) / 100.0),
					                (progress_value == 0)? 0 : 1);

				cairo_context.Rectangle (x, y, scaled_width, height);
				Gdk.CairoHelper.SetSourceColor (cairo_context, GetValueColor ());
				cairo_context.Fill ();
			}
		}
	}
}
