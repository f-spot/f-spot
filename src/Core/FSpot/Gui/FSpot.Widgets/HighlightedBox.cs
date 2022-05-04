//
// HighlightedBox.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace FSpot.Widgets
{
	public class HighlightedBox : EventBox
	{
		bool changing_style = false;

		protected HighlightedBox (IntPtr raw) : base (raw) { }

		public HighlightedBox (Widget child) : base ()
		{
			Child = child;
			AppPaintable = true;
		}

		protected override void OnStyleSet (Style style)
		{
			if (!changing_style) {
				changing_style = true;
				ModifyBg (StateType.Normal, Style.Background (StateType.Selected));
				changing_style = false;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			GdkWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal), false, 0, 0, Allocation.Width - 1, Allocation.Height - 1);
			return base.OnExposeEvent (evnt);
		}
	}
}
