//
// ThemeTestModule.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright  2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using Gtk;

namespace Hyena.Gui.Theming
{
	[TestModule ("Theme")]
	public class ThemeTestModule : Window
	{
		public ThemeTestModule () : base ("Theme")
		{
			var align = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
			var theme_widget = new ThemeTestWidget ();
			align.Add (theme_widget);
			Add (align);
			ShowAll ();

			int state = 0;
			uint[,] borders = {
				{0, 0, 0, 0},
				{10, 0, 0, 0},
				{0, 10, 0, 0},
				{0, 0, 10, 0},
				{0, 0, 0, 10},
				{10, 10, 0, 0},
				{10, 10, 10, 0},
				{10, 10, 10, 10},
				{10, 0, 0, 10},
				{0, 10, 10, 0}
			};

			GLib.Timeout.Add (2000, delegate {
				Console.WriteLine (state);
				align.TopPadding = borders[state, 0];
				align.RightPadding = borders[state, 1];
				align.BottomPadding = borders[state, 2];
				align.LeftPadding = borders[state, 3];
				if (++state % borders.GetLength (0) == 0) {
					state = 0;
				}
				return true;
			});
		}

		class ThemeTestWidget : DrawingArea
		{
			Theme theme;

			protected override void OnStyleSet (Style previous_style)
			{
				base.OnStyleSet (previous_style);
				theme = ThemeEngine.CreateTheme (this);
				theme.Context.Radius = 10;
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				Cairo.Context cr = null;
				try {
					var alloc = new Gdk.Rectangle () {
						X = Allocation.X,
						Y = Allocation.Y,
						Width = Allocation.Width,
						Height = Allocation.Height
					};
					cr = Gdk.CairoHelper.Create (evnt.Window);
					theme.DrawListBackground (cr, alloc, true);
					theme.DrawFrameBorder (cr, alloc);
				} finally {
					CairoExtensions.DisposeContext (cr);
				}
				return true;
			}
		}
	}
}
