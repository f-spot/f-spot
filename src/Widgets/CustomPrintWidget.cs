/*
 * FSpot.Widgets.CustomPrintWidget.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Mono.Unix;
using Gtk;

namespace FSpot.Widgets
{
	public class CustomPrintWidget : VBox
	{
		public delegate void ChangedHandler (Gtk.Widget widget);

		public enum FitMode {
			Zoom,
			Scaled,
			Fill,
		}

		Gtk.Image preview_image;
		CheckButton fullpage;

		RadioButton ppp1, ppp2, ppp4, ppp9;
		RadioButton zoom, fill, scaled;

		CheckButton repeat, white_border, crop_marks;
		Entry custom_text;
	
		public event ChangedHandler Changed;
		private void TriggerChanged (object sender, EventArgs e)
		{
			if (Changed != null)
				Changed (this);
		}

		public bool CropMarks {
			get { return crop_marks.Active; }
		}

		public string CustomText {
			get { return custom_text.Text; }
		}

		public FitMode Fitmode {
			get {
				if (zoom.Active)	return FitMode.Zoom;
				else if (fill.Active)	return FitMode.Fill;
				else if (scaled.Active)	return FitMode.Scaled;
				else
					throw new Exception ("Something is fucked on this GUI");
			}
		}

		public int PhotosPerPage {
			get {
				if (ppp1.Active)	return 1;
				else if (ppp2.Active)	return 2;
				else if (ppp4.Active)	return 4;
				else if (ppp9.Active)	return 9;
				else
					throw new Exception ("Something is fucked on this GUI");
			}
		}

		public Gtk.Image PreviewImage {
			get { return preview_image; }
		}

		public bool Repeat {
			get { return repeat.Active; }
		}

		public bool UseFullPage {
			get { return fullpage.Active; }
		}

		public bool WhiteBorders {
			get { return white_border.Active; }
		}

		public CustomPrintWidget () : base ()
		{
			HBox upper = new HBox ();
			preview_image = new Gtk.Image ();
			upper.PackStart (preview_image, false, false, 0);

			Frame ppp_frame = new Frame (Catalog.GetString ("Photos per page"));
			VBox vb = new VBox ();

			vb.PackStart (ppp1 = new RadioButton ("1"), false, false, 0);
			vb.PackStart (ppp2 = new RadioButton (ppp1, "2"), false, false, 0);
			vb.PackStart (ppp4 = new RadioButton (ppp1, "4"), false, false, 0);
			vb.PackStart (ppp9 = new RadioButton (ppp1, "9"), false, false, 0);
//			ppp1.Toggled += TriggerChanged;
//			ppp2.Toggled += TriggerChanged;
//			ppp4.Toggled += TriggerChanged;
//			ppp9.Toggled += TriggerChanged;

			vb.PackStart (repeat = new CheckButton (Catalog.GetString ("Repeat")), false, false, 0);
			vb.PackStart (crop_marks = new CheckButton (Catalog.GetString ("Print cut marks")), false, false, 0);
//			crop_marks.Toggled += TriggerChanged;

			ppp_frame.Child = vb;
			upper.PackStart (ppp_frame, true, true, 0);

			this.PackStart (upper, true, true, 0);
			this.PackStart (fullpage = new CheckButton (Catalog.GetString ("Full Page (no margin)")), false, false, 0);
			
			HBox hb = new HBox ();
			hb.PackStart (zoom = new RadioButton (Catalog.GetString ("Zoom")), false, false, 0);
			hb.PackStart (fill = new RadioButton (zoom, Catalog.GetString ("Fill")), false, false, 0);
			hb.PackStart (scaled = new RadioButton (zoom, Catalog.GetString ("Scaled")), false, false, 0);
			this.PackStart (hb, false, false, 0);
			zoom.Toggled += TriggerChanged;
			fill.Toggled += TriggerChanged;
			scaled.Toggled += TriggerChanged;

			this.PackStart (white_border = new CheckButton (Catalog.GetString ("White borders")), false, false, 0);
			white_border.Toggled += TriggerChanged;

			hb = new HBox ();
			hb.PackStart (new Label (Catalog.GetString ("Custom Text: ")), false, false, 0);

			hb.PackStart (custom_text = new Entry (), true, true, 0);
			this.PackStart (hb, false, false, 0);
			TriggerChanged (this, null);
		}
	}
}
