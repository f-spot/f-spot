/*
 * ScalingIconView.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 */

using System;
using FSpot.Core;

namespace FSpot.Widgets {
	public class ScalingIconView : IconView {
		protected ScalingIconView (IntPtr raw) : base (raw) {}

		public ScalingIconView () : base () { }
		public ScalingIconView (IBrowsableCollection collection) : base (collection) { }

		protected override void UpdateLayout ()
		{
			int num_thumbnails = collection != null ? collection.Count : 0;
			cells_per_row = System.Math.Max (num_thumbnails, 1);

			int num_rows = 1;
			int num_cols = num_thumbnails;

			int available_height = Allocation.Height - 2 * BORDER_SIZE;
			if (DisplayTags)
				available_height -= tag_icon_size + tag_icon_vspacing;

			if (DisplayDates && this.Style != null) {
				Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription, Pango.Language.FromString ("en_US"));
				available_height -= PangoPixels (metrics.Ascent + metrics.Descent);
			}

			thumbnail_width = (int) (available_height / thumbnail_ratio);

			cell_width = ThumbnailWidth + 2 * cell_border_width;
			cell_height = ThumbnailHeight + 2 * cell_border_width;

			SetSize (System.Math.Max (((uint) (num_cols * cell_width + 2 * BORDER_SIZE)), (uint)Allocation.Width), (uint) (num_rows * cell_height + 2 * BORDER_SIZE));

			Vadjustment.StepIncrement = cell_height;
			Vadjustment.Change ();

			Hadjustment.StepIncrement = cell_width;
			Hadjustment.Change ();
		}
	}
}
