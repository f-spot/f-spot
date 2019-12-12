//
// HighlightedBox.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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
	public class HighlightedBox : EventBox
	{
		bool changing_style = false;

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
