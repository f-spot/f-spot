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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;

using FSpot.Query;
using FSpot.Widgets;

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
