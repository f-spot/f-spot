namespace FSpot {
	public class ScalingIconView : IconView {
		public ScalingIconView () : base () { }
 		public ScalingIconView (IBrowsableCollection collection) : base (collection) { }

		
		protected override void UpdateLayout ()
		{
			System.Console.WriteLine ("in update layout {0}", Allocation.ToString ());

			int num_thumbnails;
			if (collection != null)
				num_thumbnails = collection.Items.Length;
			else
				num_thumbnails = 0;

			cells_per_row = System.Math.Max (num_thumbnails, 1);
			
			int num_rows = 1;
			int num_cols = num_thumbnails;

			int available_height = Allocation.Height - 2 * BORDER_SIZE;
			if (DisplayTags)
				available_height -= TAG_ICON_SIZE + TAG_ICON_VSPACING;
			
			if (DisplayDates && this.Style != null) {
				Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription, 
											  Pango.Language.FromString ("en_US"));
				available_height -= PangoPixels (metrics.Ascent + metrics.Descent);
			}
			
			thumbnail_width = (int) (available_height / thumbnail_ratio);

			cell_width = ThumbnailWidth + 2 * CELL_BORDER_WIDTH;
			cell_height = ThumbnailHeight + 2 * CELL_BORDER_WIDTH;
			
			SetSize (System.Math.Max (((uint) (num_cols * cell_width + 2 * BORDER_SIZE)), (uint)Allocation.Width), (uint) (num_rows * cell_height + 2 * BORDER_SIZE));

			Vadjustment.StepIncrement = cell_height;
			Vadjustment.Change ();

			Hadjustment.StepIncrement = cell_width;
			Hadjustment.Change ();
		}
#if false		
		protected override void UpdateLayout ()
		{
			if (collection != null) {
				int total = collection.Items.Length;

				if (total > 0)
					thumbnail_width = (Allocation.Width - (total * 2 * CELL_BORDER_WIDTH) - 2 * BORDER_SIZE)/ total;
			}

			base.UpdateLayout ();
		}
#endif

	}
}
