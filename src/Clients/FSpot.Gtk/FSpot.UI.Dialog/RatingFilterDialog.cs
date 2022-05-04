//
// RatingFilterDialog.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2008-2009 Lorenzo Milesi
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Query;
using FSpot.Widgets;

using Gtk;

namespace FSpot.UI.Dialog
{
	public class RatingFilterDialog : BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] HBox minrating_hbox;
		[GtkBeans.Builder.Object] HBox maxrating_hbox;
#pragma warning restore 649

		int minrating_value = 4;
		int maxrating_value = 5;
		RatingEntry minrating;
		RatingEntry maxrating;

		public RatingFilterDialog (FSpot.PhotoQuery query, Gtk.Window parent_window)
			: base ("RatingFilterDialog.ui", "rating_filter_dialog")
		{
			TransientFor = parent_window;
			DefaultResponse = ResponseType.Ok;
			ok_button.GrabFocus ();

			if (query.RatingRange != null) {
				minrating_value = (int)query.RatingRange.MinRating;
				maxrating_value = (int)query.RatingRange.MaxRating;
			}
			minrating = new RatingEntry (minrating_value);
			maxrating = new RatingEntry (maxrating_value);
			minrating_hbox.PackStart (minrating, false, false, 0);
			maxrating_hbox.PackStart (maxrating, false, false, 0);

			minrating.Show ();
			maxrating.Show ();

			minrating.Changed += HandleMinratingChanged;
			maxrating.Changed += HandleMaxratingChanged;

			var response = (ResponseType)Run ();

			if (response == ResponseType.Ok) {
				query.RatingRange = new RatingRange ((uint)minrating.Value, (uint)maxrating.Value);
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
