//
// WrapLabel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Widgets
{
	public class WrapLabel : Widget
	{
		string text;
		bool use_markup = false;
		bool wrap = true;
		Pango.Layout layout;

		public WrapLabel ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
		}

		void CreateLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
			}

			layout = new Pango.Layout (PangoContext);
			layout.Wrap = Pango.WrapMode.Word;
		}

		void UpdateLayout ()
		{
			if (layout == null) {
				CreateLayout ();
			}

			layout.Ellipsize = wrap ? Pango.EllipsizeMode.None : Pango.EllipsizeMode.End;

			if (text == null) {
				text = "";
			}

			if (use_markup) {
				layout.SetMarkup (text);
			} else {
				layout.SetText (text);
			}

			QueueResize ();
		}

		protected override void OnStyleSet (Style previous_style)
		{
			CreateLayout ();
			UpdateLayout ();
			base.OnStyleSet (previous_style);
		}

		protected override void OnRealized ()
		{
			GdkWindow = Parent.GdkWindow;
			base.OnRealized ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{

			layout.Width = (int)(allocation.Width * Pango.Scale.PangoScale);
			layout.GetPixelSize (out var lw, out var lh);

			TooltipText = layout.IsEllipsized ? text : null;
			HeightRequest = lh;

			base.OnSizeAllocated (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (evnt.Window == GdkWindow) {
				// Center the text vertically
				layout.GetPixelSize (out var lw, out var lh);
				int y = Allocation.Y + (Allocation.Height - lh) / 2;

				Gtk.Style.PaintLayout (Style, GdkWindow, State, false,
					evnt.Area, this, null, Allocation.X, y, layout);
			}

			return true;
		}

		public void MarkupFormat (string format, params object[] args)
		{
			if (args == null || args.Length == 0) {
				Markup = format;
				return;
			}

			for (int i = 0; i < args.Length; i++) {
				if (args[i] is string) {
					args[i] = GLib.Markup.EscapeText ((string)args[i]);
				}
			}

			Markup = string.Format (format, args);
		}

		public bool Wrap {
			get { return wrap; }
			set {
				wrap = value;
				UpdateLayout ();
			}
		}

		public string Markup {
			get { return text; }
			set {
				use_markup = true;
				text = value;
				UpdateLayout ();
			}
		}

		public string Text {
			get { return text; }
			set {
				use_markup = false;
				text = value;
				UpdateLayout ();
			}
		}
	}
}
