// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Models;
using FSpot.Settings;

using Gdk;

using Gtk;

namespace FSpot.Widgets
{
	public class ThumbnailTagsCaptionRenderer : ThumbnailCaptionRenderer
	{
		readonly int tagIconSize;
		readonly int tagIconHSpacing;

		public ThumbnailTagsCaptionRenderer () : this (16)
		{
		}

		public ThumbnailTagsCaptionRenderer (int tagIconSize) : this (tagIconSize, 2)
		{
		}

		public ThumbnailTagsCaptionRenderer (int tagIconSize, int tagIconHSpacing)
		{
			this.tagIconSize = tagIconSize;
			this.tagIconHSpacing = tagIconHSpacing;
		}

		public override int GetHeight (Widget widget, int width)
		{
			return tagIconSize;
		}

		public override void Render (Drawable window, Widget widget, Rectangle cellArea,
									 Rectangle exposeArea, StateType cellState, IPhoto photo)
		{
			var tags = photo.Tags;
			Rectangle tag_bounds;

			tag_bounds.X = cellArea.X + (cellArea.Width + tagIconHSpacing - tags.Count * (tagIconSize + tagIconHSpacing)) / 2;
			tag_bounds.Y = cellArea.Y;// + cellArea.Height - cell_border_width - tagIconSize + tag_icon_vspacing;
			tag_bounds.Width = tagIconSize;
			tag_bounds.Height = tagIconSize;


			foreach (Tag t in tags) {

				if (t == null)
					continue;

				Pixbuf icon = t.TagIcon.Icon;

				Tag tagIter = t.Category;
				while (icon == null && tagIter != Constants.RootCategory && tagIter != null) {
					icon = tagIter.TagIcon.Icon;
					tagIter = tagIter.Category;
				}

				if (icon == null)
					continue;

				if (tag_bounds.Intersect (exposeArea, out var region)) {
					Pixbuf scaled_icon;
					if (icon.Width == tag_bounds.Width) {
						scaled_icon = icon;
					} else {
						scaled_icon = icon.ScaleSimple (tag_bounds.Width, tag_bounds.Height, InterpType.Bilinear);
					}

					if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screen_profile))
						FSpot.ColorManagement.ApplyProfile (scaled_icon, screen_profile);

					scaled_icon.RenderToDrawable (window, widget.Style.WhiteGC,
							region.X - tag_bounds.X,
							region.Y - tag_bounds.Y,
							region.X, region.Y,
							region.Width, region.Height,
							RgbDither.None, region.X, region.Y);

					if (scaled_icon != icon) {
						scaled_icon.Dispose ();
					}
				}

				tag_bounds.X += tag_bounds.Width + tagIconHSpacing;
			}
		}
	}
}

