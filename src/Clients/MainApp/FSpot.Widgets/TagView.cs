//
// TagView.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Iain Churcher <iain.linux.coding@googlemail.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2004-2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2010 Iain Churcher
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

using FSpot.Core;
using FSpot.Settings;

namespace FSpot.Widgets
{
	public class TagView : EventBox
	{
		int thumbnail_size = 20;
		IPhoto photo;
		Tag [] tags;
		static int TAG_ICON_VSPACING = 5;

		bool HideTags {
			get {
				return (Preferences.Get<int> (Preferences.TAG_ICON_SIZE) == (int)FSpot.Settings.IconSize.Hidden);
			}
		}

		public TagView ()
		{
			VisibleWindow = false;
		}

		protected TagView (IntPtr raw) : base (raw)
		{
			VisibleWindow = false;
		}

		public IPhoto Current {
			set {
				photo = value;

				if (photo != null && photo.Tags != null && !HideTags) {
					SetSizeRequest ((thumbnail_size + TAG_ICON_VSPACING) * photo.Tags.Length,
							thumbnail_size);
				} else {
					SetSizeRequest (0, thumbnail_size);
				}
				QueueResize ();
				QueueDraw ();
			}
		}

		public Tag [] Tags {
			get { return tags; }
			set {
				tags = value;
				QueueDraw ();
			}
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			if (photo != null)
				tags = photo.Tags;

			if (tags == null || HideTags) {
				SetSizeRequest (0, thumbnail_size);
				return base.OnExposeEvent (args);
			}

			DrawTags ();

			return base.OnExposeEvent (args);
		}

		public void DrawTags ()
		{
			if (tags == null)
				return;

			SetSizeRequest ((thumbnail_size + TAG_ICON_VSPACING) * tags.Length,
					thumbnail_size);

			int tag_x = Allocation.X;
			int tag_y = Allocation.Y + (Allocation.Height - thumbnail_size) / 2;

			string [] names = new string [tags.Length];
			int i = 0;
			foreach (Tag t in tags) {
				names [i++] = t.Name;

				Pixbuf icon = t.Icon;

				Category category = t.Category;
				while (icon == null && category != null) {
					icon = category.Icon;
					category = category.Category;
				}

				if (icon == null)
					continue;

				Pixbuf scaled_icon;
				if (icon.Width == thumbnail_size) {
					scaled_icon = icon;
				} else {
					scaled_icon = icon.ScaleSimple (thumbnail_size, thumbnail_size, InterpType.Bilinear);
				}
				Cms.Profile screen_profile;
				if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile))
					FSpot.ColorManagement.ApplyProfile (scaled_icon, screen_profile);

				scaled_icon.RenderToDrawable (GdkWindow, Style.WhiteGC,
								  0, 0, tag_x, tag_y, thumbnail_size, thumbnail_size,
								  RgbDither.None, tag_x, tag_y);
				tag_x += thumbnail_size + TAG_ICON_VSPACING;
			}

			TooltipText = string.Join (", ", names);
		}
	}
}
