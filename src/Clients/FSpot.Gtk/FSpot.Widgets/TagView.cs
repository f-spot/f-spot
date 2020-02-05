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
using System.Collections.Generic;
using System.Linq;

using Gdk;
using Gtk;

using FSpot.Cms;
using FSpot.Core;
using FSpot.Settings;

namespace FSpot.Widgets
{
	public class TagView : EventBox
	{
		readonly int thumbnailSize = 20;
		IPhoto photo;
		IEnumerable<Tag> tags;
		static readonly int TAG_ICON_VSPACING = 5;

		bool HideTags {
			get {
				return (Preferences.Get<int> (Preferences.TagIconSize) == (int)FSpot.Settings.IconSize.Hidden);
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

				if (photo?.Tags != null && !HideTags) {
					SetSizeRequest ((thumbnailSize + TAG_ICON_VSPACING) * photo.Tags.Count(),
							thumbnailSize);
				} else {
					SetSizeRequest (0, thumbnailSize);
				}
				QueueResize ();
				QueueDraw ();
			}
		}

		public IEnumerable<Tag> Tags {
			get { return tags; }
			set {
				tags = value.ToList ();
				QueueDraw ();
			}
		}

		protected override bool OnExposeEvent (EventExpose args)
		{
			if (photo != null)
				tags = photo.Tags;

			if (tags == null || HideTags) {
				SetSizeRequest (0, thumbnailSize);
				return base.OnExposeEvent (args);
			}

			DrawTags ();

			return base.OnExposeEvent (args);
		}

		public void DrawTags ()
		{
			if (tags == null)
				return;

			SetSizeRequest ((thumbnailSize + TAG_ICON_VSPACING) * tags.Count (), thumbnailSize);

			int tagX = Allocation.X;
			int tagY = Allocation.Y + (Allocation.Height - thumbnailSize) / 2;

			string [] names = new string [tags.Count ()];
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

				Pixbuf scaledIcon;
				if (icon.Width == thumbnailSize) {
					scaledIcon = icon;
				} else {
					scaledIcon = icon.ScaleSimple (thumbnailSize, thumbnailSize, InterpType.Bilinear);
				}

				if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screenProfile))
					FSpot.ColorManagement.ApplyProfile (scaledIcon, screenProfile);

				scaledIcon.RenderToDrawable (GdkWindow, Style.WhiteGC,
								  0, 0, tagX, tagY, thumbnailSize, thumbnailSize,
								  RgbDither.None, tagX, tagY);
				tagX += thumbnailSize + TAG_ICON_VSPACING;
			}

			TooltipText = string.Join (", ", names);
		}
	}
}
