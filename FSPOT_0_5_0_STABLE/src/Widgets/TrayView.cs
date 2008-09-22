/*
 * TrayView.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 *
 * Copyright (C) 2004 Novell, Inc.
 * This is free software. See COPYING for details.
 */

namespace FSpot.Widgets
{
	public class TrayView : IconView {
		public TrayView (System.IntPtr raw) : base (raw) {}
	
		public TrayView (FSpot.IBrowsableCollection query) : base (query) 
		{
			DisplayDates = false;
			DisplayTags = false;
			cell_border_width = 10;
			tag_icon_vspacing = 0;
			tag_icon_size = 0;
		}
		
		protected override void UpdateLayout ()
		{
			//DisplayDates = false;
			//DisplayTags = false;
	
			int total_rows;
			int available_width = Allocation.Width - 2 * BORDER_SIZE;
			cells_per_row = System.Math.Max (available_width / 256, 1);
			int width = 0;
			//int height = 0;
			
			do {
				cell_width = System.Math.Min (256, available_width / cells_per_row);
				width = cell_width - 2 * cell_border_width;
				cell_height = (int)(width / ThumbnailRatio) + 2 * cell_border_width;
				total_rows = (int) System.Math.Ceiling (collection.Count / (double)cells_per_row);
				cells_per_row ++;
			} while (total_rows > Allocation.Height / cell_height);
			cells_per_row --;
	
	
			if (width != ThumbnailWidth) {
				thumbnail_width = width;
			}
	
			//base.UpdateLayout ();
		}
	}
}
