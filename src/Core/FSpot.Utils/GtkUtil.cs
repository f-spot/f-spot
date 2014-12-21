//
// GtkUtil.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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
using Gdk;

namespace FSpot.Utils
{
	public static class GtkUtil
	{
		public static MenuItem MakeMenuItem (Menu menu, string l, EventHandler e, bool enabled = true)
		{
			MenuItem i;
			StockItem item = StockItem.Zero;
	
			if (StockManager.Lookup (l, ref item)) {
				i = new ImageMenuItem (l, new AccelGroup ());
			} else {
				i = new MenuItem (l);
			}
	
			if (e != null)
				i.Activated += e;
	
			i.Sensitive = enabled;
			
			menu.Append (i);
			i.Show ();
	
			return i;
		}

		public static MenuItem MakeMenuItem (Menu menu, string label, string imageName, EventHandler e, bool enabled)
		{
			var i = new ImageMenuItem (label);
			i.Activated += e;
			i.Sensitive = enabled;
			i.Image = Image.NewFromIconName (imageName, IconSize.Menu);
			
			menu.Append (i);
			i.Show ();
	
			return i;
		}

		public static MenuItem MakeCheckMenuItem (Menu menu, string label, EventHandler e, bool enabled, bool active, bool asRadio)
		{
			var i = new CheckMenuItem (label);
			i.Sensitive = enabled;
			i.DrawAsRadio = asRadio;
			i.Active = active;

			i.Activated += e;
	
			menu.Append (i);
			i.Show ();
	
			return i;
		}

		public static void MakeMenuSeparator (Menu menu)
		{
			var i = new SeparatorMenuItem ();
			menu.Append (i);
			i.Show ();
		}

		public static ToolButton ToolButtonFromTheme (string themeId, string label, bool important)
		{
			var button = new ToolButton (null, null);
			button.Label = label;
			button.IconName = themeId;
			button.IsImportant = important;
			button.UseUnderline = true;
			return button;
		}

		public static Pixbuf TryLoadIcon (IconTheme theme, string[] names, int size, IconLookupFlags flags)
		{
			try {
				var info = theme.ChooseIcon (names, size, flags);
				return info.LoadIcon ();
			} catch {
				try {
					return theme.LoadIcon ("gtk-missing-image", size, flags);
				} catch {
					return null;
				}
			}	
		}

		public static Pixbuf TryLoadIcon (IconTheme theme, string iconName, int size, IconLookupFlags flags)
		{
			try {
				return theme.LoadIcon (iconName, size, flags);
			} catch {
				try {
					return theme.LoadIcon ("gtk-missing-image", size, flags);
				} catch {
					return null;
				}
			}	
		}

		public static void ModifyColors (Widget widget)
		{
			try {
				widget.ModifyFg (StateType.Normal, widget.Style.TextColors [(int)StateType.Normal]);
				widget.ModifyFg (StateType.Active, widget.Style.TextColors [(int)StateType.Active]);
				widget.ModifyFg (StateType.Selected, widget.Style.TextColors [(int)StateType.Selected]);

				widget.ModifyBg (StateType.Normal, widget.Style.BaseColors [(int)StateType.Normal]);
				widget.ModifyBg (StateType.Active, widget.Style.BaseColors [(int)StateType.Active]);
				widget.ModifyBg (StateType.Selected, widget.Style.BaseColors [(int)StateType.Selected]);
				
			} catch {
				widget.ModifyFg (StateType.Normal, widget.Style.Black);
				widget.ModifyBg (StateType.Normal, widget.Style.Black);
			}
		}
	}
}
