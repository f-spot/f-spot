/*
 * Rating.cs
 *
 * Author[s]
 * 	Bengt Thuree <bengt@thuree.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using Gtk;
using Gnome;
using FSpot;
using FSpot.Query;
using FSpot.Widgets;
using FSpot.UI.Dialog;

public class RatingFilter {
	public class Set : GladeDialog {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private HBox minrating_hbox;
		[Glade.Widget] private HBox maxrating_hbox;
		
		private int minrating_value = 4;
		private int maxrating_value = 5;
		private Rating minrating;
		private Rating maxrating;

		public Set (FSpot.PhotoQuery query, Gtk.Window parent_window)
		{
			this.query = query;
			this.parent_window = parent_window;
		}

		public bool Execute ()
		{
			this.CreateDialog ("rating_filter_dialog");
			
			if (query.RatingRange != null) {
				minrating_value = (int) query.RatingRange.MinRating;
				maxrating_value = (int) query.RatingRange.MaxRating;
			}
			minrating = new Rating (minrating_value);
			maxrating = new Rating (maxrating_value);
			minrating_hbox.PackStart (minrating, false, false, 0);
			maxrating_hbox.PackStart (maxrating, false, false, 0);

			Dialog.TransientFor = parent_window;
			Dialog.DefaultResponse = ResponseType.Ok;

			ResponseType response = (ResponseType) this.Dialog.Run ();

			bool success = false;

			if (response == ResponseType.Ok) {
				query.RatingRange = new RatingRange ((uint) minrating.Value, (uint) maxrating.Value);
				success = true;
			}
			
			this.Dialog.Destroy ();
			return success;
		}
	}
}


