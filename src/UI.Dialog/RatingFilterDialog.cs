/*
 * FSpot.UI.Dialog.RatingFilterDialog.cs
 *
 * Author(s):
 * 	Bengt Thuree <bengt@thuree.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using Gtk;
using FSpot;
using FSpot.Query;
using FSpot.Widgets;
using FSpot.UI.Dialog;

namespace FSpot.UI.Dialog
{
	public class RatingFilterDialog : BuilderDialog {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] HBox minrating_hbox;
		[GtkBeans.Builder.Object] HBox maxrating_hbox;
		
		private int minrating_value = 4;
		private int maxrating_value = 5;
		private Rating minrating;
		private Rating maxrating;

		public RatingFilterDialog (FSpot.PhotoQuery query, Gtk.Window parent_window) : base ("RatingFilterDialog.ui", "rating_filter_dialog")
		{
			this.query = query;
			this.parent_window = parent_window;
			TransientFor = parent_window;
			DefaultResponse = ResponseType.Ok;
			ok_button.GrabFocus ();
			
			if (query.RatingRange != null) {
				minrating_value = (int) query.RatingRange.MinRating;
				maxrating_value = (int) query.RatingRange.MaxRating;
			}
			minrating = new Rating (minrating_value);
			maxrating = new Rating (maxrating_value);
			minrating_hbox.PackStart (minrating, false, false, 0);
			maxrating_hbox.PackStart (maxrating, false, false, 0);

			ResponseType response = (ResponseType) Run ();

			if (response == ResponseType.Ok) {
				query.RatingRange = new RatingRange ((uint) minrating.Value, (uint) maxrating.Value);
			}
			
			Destroy ();
		}
	}
}


