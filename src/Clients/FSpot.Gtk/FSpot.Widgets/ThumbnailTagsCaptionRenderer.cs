//
// ThumbnailTagsCaptionRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
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
		readonly int tag_icon_size;
		readonly int tag_icon_hspacing;

		public ThumbnailTagsCaptionRenderer () : this (16)
		{
		}

		public ThumbnailTagsCaptionRenderer (int tag_icon_size) : this (tag_icon_size, 2)
		{
		}

		public ThumbnailTagsCaptionRenderer (int tag_icon_size, int tag_icon_hspacing)
		{
			this.tag_icon_size = tag_icon_size;
			this.tag_icon_hspacing = tag_icon_hspacing;
		}

		#region Drawing Methods

		public override int GetHeight (Widget widget, int width)
		{
			return tag_icon_size;
		}

		public override void Render (Drawable window,
									 Widget widget,
									 Rectangle cell_area,
									 Rectangle expose_area,
									 StateType cell_state,
									 IPhoto photo)
		{
			var tags = photo.Tags;
			Rectangle tag_bounds;

			tag_bounds.X = cell_area.X + (cell_area.Width + tag_icon_hspacing - tags.Count * (tag_icon_size + tag_icon_hspacing)) / 2;
			tag_bounds.Y = cell_area.Y;// + cell_area.Height - cell_border_width - tag_icon_size + tag_icon_vspacing;
			tag_bounds.Width = tag_icon_size;
			tag_bounds.Height = tag_icon_size;


			foreach (Tag t in tags) {

				if (t == null)
					continue;

				Pixbuf icon = t.TagIcon.Icon;

				Tag tag_iter = t.Category;
				while (icon == null && tag_iter != App.Instance.Database.Tags.RootCategory && tag_iter != null) {
					icon = tag_iter.TagIcon.Icon;
					tag_iter = tag_iter.Category;
				}

				if (icon == null)
					continue;

				if (tag_bounds.Intersect (expose_area, out var region)) {
					Pixbuf scaled_icon;
					if (icon.Width == tag_bounds.Width) {
						scaled_icon = icon;
					} else {
						scaled_icon = icon.ScaleSimple (tag_bounds.Width,
								tag_bounds.Height,
								InterpType.Bilinear);
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

				tag_bounds.X += tag_bounds.Width + tag_icon_hspacing;
			}
		}

		#endregion

	}
}

