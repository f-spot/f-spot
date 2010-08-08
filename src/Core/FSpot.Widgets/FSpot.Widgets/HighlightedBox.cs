/*
 * FSpot.Widgets.HighlightedBox.cs
 *
 * Author(s)
 *  Gabriel Burt  <gabriel.burt@gmail.com>
 * 
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;

namespace FSpot.Widgets
{
	public class HighlightedBox : EventBox
	{
		private bool changing_style = false;

		protected HighlightedBox (IntPtr raw) : base (raw) {}

		public HighlightedBox (Widget child) : base ()
		{
			Child = child;
			AppPaintable = true;
		}

		protected override void OnStyleSet(Style style)
		{
			if (!changing_style) {
				changing_style = true;
				ModifyBg(StateType.Normal, Style.Background(StateType.Selected));
				changing_style = false;
			}
		}

		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			GdkWindow.DrawRectangle(Style.ForegroundGC(StateType.Normal), false, 0, 0, Allocation.Width - 1, Allocation.Height - 1);
			return base.OnExposeEvent(evnt);
		}
	}
}
