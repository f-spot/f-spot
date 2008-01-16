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

public class RatingFilter {
	public class Set : FSpot.GladeDialog {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private SpinButton minrating;
		[Glade.Widget] private SpinButton maxrating;

		public Set (FSpot.PhotoQuery query, Gtk.Window parent_window)
		{
			this.query = query;
			this.parent_window = parent_window;
		}

		public bool Execute ()
		{
			this.CreateDialog ("set_rating_filter");
			
			if (query.RatingRange != null) {
				minrating.Value = query.RatingRange.MinRating;
				maxrating.Value = query.RatingRange.MaxRating;
			}

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


