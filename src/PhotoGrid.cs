using System;

namespace FSpot {
	private class PhotoGrid : Table {
		const int NUM_COLUMNS = 5;
		const int NUM_ROWS = 4;
		
	        const int CELL_WIDTH = 128;
		const int CELL_HEIGHT = 96;

		const int PADDING = 3;

		Gtk.Image [] image_widgets;

		int position = -1;
		
		/*
		 * This controls whether or wraps as new photos are added.
		 */
		public bool Scroll = false;

		public PhotoGrid () : base (NUM_ROWS, NUM_COLUMNS, true)
		{
			image_widgets = new Gtk.Image [NUM_ROWS * NUM_COLUMNS];

			int i = 0;
			for (uint j = 0; j < NUM_ROWS; j++) {
				for (uint k = 0; k < NUM_COLUMNS; k ++) {
					Gtk.Image image_widget = new Gtk.Image ();

					image_widget = new Gtk.Image ();
					image_widget.SetSizeRequest (CELL_WIDTH, CELL_HEIGHT);

					Attach (image_widget, k, k + 1, j, j + 1, 0, 0, PADDING, PADDING);

					image_widgets [i] = image_widget;

					i ++;
				}
			}
		}

		private void ScrollIfNeeded ()
		{
			if (position < NUM_COLUMNS * NUM_ROWS)
				return;

			for (int i = 0; i < NUM_COLUMNS * (NUM_ROWS - 1); i ++) {
				image_widgets [i].Pixbuf = image_widgets [i + NUM_COLUMNS].Pixbuf;
			}

			for (int i = NUM_COLUMNS * (NUM_ROWS - 1); i < NUM_COLUMNS * NUM_ROWS; i ++) {
				// FIXME: Lame, apparently I can't set the Pixbuf property to null.
				// GTK# bug?
				image_widgets [i].Pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, 1, 1);
			}

			position -= NUM_COLUMNS;
		}

		public void AddThumbnail (Pixbuf thumbnail)
		{
			Pixbuf scaled_thumbnail;

			if (thumbnail.Width <= CELL_WIDTH && thumbnail.Height <= CELL_HEIGHT) {
				scaled_thumbnail = thumbnail;
			} else {
				int thumbnail_width, thumbnail_height;

				PixbufUtils.Fit (thumbnail, CELL_WIDTH, CELL_HEIGHT, false,
						 out thumbnail_width, out thumbnail_height);
				scaled_thumbnail = thumbnail.ScaleSimple (thumbnail_width, thumbnail_height,
									  Gdk.InterpType.Bilinear);
			}

			position = (position + 1) % (NUM_COLUMNS * NUM_ROWS);
			if (Scroll)
				ScrollIfNeeded ();

			image_widgets [position].Pixbuf = scaled_thumbnail;
			if (Scroll)
				position ++;


			if (scaled_thumbnail != thumbnail)
				scaled_thumbnail.Dispose ();
			
		}
	}
}
