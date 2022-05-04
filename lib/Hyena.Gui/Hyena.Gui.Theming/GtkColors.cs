//
// GtkColors.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Gui.Theming
{
	public enum GtkColorClass
	{
		Light,
		Mid,
		Dark,
		Base,
		Text,
		Background,
		Foreground
	}

	public class GtkColors
	{
		Cairo.Color[] gtk_colors;
		Widget widget;
		bool refreshing = false;

		public event EventHandler Refreshed;

		public Widget Widget {
			get { return widget; }
			set {
				if (widget == value) {
					return;
				} else if (widget != null) {
					widget.Realized -= OnWidgetRealized;
					widget.StyleSet -= OnWidgetStyleSet;
				}

				widget = value;

				if (widget.IsRealized) {
					RefreshColors ();
				}

				widget.Realized += OnWidgetRealized;
				widget.StyleSet += OnWidgetStyleSet;
			}
		}

		public GtkColors ()
		{
		}

		void OnWidgetRealized (object o, EventArgs args)
		{
			RefreshColors ();
		}

		void OnWidgetStyleSet (object o, StyleSetArgs args)
		{
			RefreshColors ();
		}

		public Cairo.Color GetWidgetColor (GtkColorClass @class, StateType state)
		{
			if (gtk_colors == null) {
				RefreshColors ();
			}

			return gtk_colors[(int)@class * ((int)StateType.Insensitive + 1) + (int)state];
		}

		public void RefreshColors ()
		{
			if (refreshing) {
				return;
			}

			refreshing = true;

			int sn = (int)StateType.Insensitive + 1;
			int cn = (int)GtkColorClass.Foreground + 1;

			if (gtk_colors == null) {
				gtk_colors = new Cairo.Color[sn * cn];
			}

			for (int c = 0, i = 0; c < cn; c++) {
				for (int s = 0; s < sn; s++, i++) {
					Gdk.Color color = Gdk.Color.Zero;

					if (widget != null && widget.IsRealized) {
						switch ((GtkColorClass)c) {
						case GtkColorClass.Light: color = widget.Style.LightColors[s]; break;
						case GtkColorClass.Mid: color = widget.Style.MidColors[s]; break;
						case GtkColorClass.Dark: color = widget.Style.DarkColors[s]; break;
						case GtkColorClass.Base: color = widget.Style.BaseColors[s]; break;
						case GtkColorClass.Text: color = widget.Style.TextColors[s]; break;
						case GtkColorClass.Background: color = widget.Style.Backgrounds[s]; break;
						case GtkColorClass.Foreground: color = widget.Style.Foregrounds[s]; break;
						}
					} else {
						color = new Gdk.Color (0, 0, 0);
					}

					gtk_colors[c * sn + s] = CairoExtensions.GdkColorToCairoColor (color);
				}
			}

			OnRefreshed ();

			refreshing = false;
		}

		protected virtual void OnRefreshed ()
		{
			Refreshed?.Invoke (this, EventArgs.Empty);
		}
	}
}
