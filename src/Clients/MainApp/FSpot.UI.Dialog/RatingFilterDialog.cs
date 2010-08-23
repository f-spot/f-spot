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
		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] HBox minrating_hbox;
		[GtkBeans.Builder.Object] HBox maxrating_hbox;

		private int minrating_value = 4;
		private int maxrating_value = 5;
		private RatingEntry minrating;
		private RatingEntry maxrating;

		public RatingFilterDialog (FSpot.PhotoQuery query, Gtk.Window parent_window)
            : base ("RatingFilterDialog.ui", "rating_filter_dialog")
		{
			TransientFor = parent_window;
			DefaultResponse = ResponseType.Ok;
			ok_button.GrabFocus ();

			if (query.RatingRange != null) {
				minrating_value = (int) query.RatingRange.MinRating;
				maxrating_value = (int) query.RatingRange.MaxRating;
			}
			minrating = new RatingEntry (minrating_value);
			maxrating = new RatingEntry (maxrating_value);
			minrating_hbox.PackStart (minrating, false, false, 0);
			maxrating_hbox.PackStart (maxrating, false, false, 0);

            minrating.Show ();
            maxrating.Show ();

            minrating.Changed += HandleMinratingChanged;
            maxrating.Changed += HandleMaxratingChanged;

			ResponseType response = (ResponseType) Run ();

			if (response == ResponseType.Ok) {
				query.RatingRange = new RatingRange ((uint) minrating.Value, (uint) maxrating.Value);
			}

			Destroy ();
		}

        void HandleMinratingChanged (object sender, System.EventArgs e)
        {
            if (minrating.Value > maxrating.Value) {
                maxrating.Changed -= HandleMaxratingChanged;
                maxrating.Value = minrating.Value;
                maxrating.Changed += HandleMaxratingChanged;
            }
        }

        void HandleMaxratingChanged (object sender, System.EventArgs e)
        {
            if (maxrating.Value < minrating.Value) {
                minrating.Changed -= HandleMinratingChanged;
                minrating.Value = maxrating.Value;
                minrating.Changed += HandleMinratingChanged;
            }
        }
	}
}
