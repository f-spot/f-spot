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
using FSpot.Utils;

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

		RadioButton ppp1, ppp2, ppp4, ppp9, ppp20, ppp30;
		RadioButton zoom, fill, scaled;

		CheckButton repeat, white_border, crop_marks;
		Entry custom_text;
	
		PrintOperation print_operation;

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
					throw new Exception ("Something is wrong on this GUI");
			}
		}

		public int PhotosPerPage {
			get {
				if (ppp1.Active)	return 1;
				else if (ppp2.Active)	return 2;
				else if (ppp4.Active)	return 4;
				else if (ppp9.Active)	return 9;
				else if (ppp20.Active)	return 20;
				else if (ppp30.Active)	return 30;
				else
					throw new Exception ("Something is wrong on this GUI");
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

		public CustomPrintWidget (PrintOperation print_operation) : base ()
		{
			this.print_operation = print_operation;

			HBox upper = new HBox ();
			preview_image = new Gtk.Image ();
			upper.PackStart (preview_image, false, false, 0);

			Frame page_size = new Frame (Catalog.GetString ("Page Setup"));
			VBox vb = new VBox ();
			Label current_settings = new Label ();
			if (FSpot.Global.PageSetup != null)
				current_settings.Text = String.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			else
				current_settings.Text = String.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), "...", "...");

			vb.PackStart (current_settings, false, false, 0);
			Button page_setup_btn = new Button (Catalog.GetString ("Set Page Size and Orientation"));
			page_setup_btn.Clicked += delegate {
				this.print_operation.DefaultPageSetup = Print.RunPageSetupDialog (null, print_operation.DefaultPageSetup, this.print_operation.PrintSettings); 
				current_settings.Text = String.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			};
			vb.PackStart (page_setup_btn, false, false, 0);

			page_size.Add (vb);


			VBox right_vb = new VBox ();
			right_vb.PackStart (page_size, true, true, 0);

			Frame tbl_frame = new Frame (Catalog.GetString ("Photos per page"));
			Table tbl = new Table (2, 7, false);

			tbl.Attach (ppp1 = new RadioButton ("1"), 0, 1, 1, 2);
			tbl.Attach (ppp2 = new RadioButton (ppp1, "2"), 0, 1, 2, 3);
			tbl.Attach (ppp4 = new RadioButton (ppp1, "2 x 2"), 0, 1, 3, 4);
			tbl.Attach (ppp9 = new RadioButton (ppp1, "3 x 3"), 0, 1, 4, 5);
			tbl.Attach (ppp20 = new RadioButton (ppp1, "4 x 5"), 0, 1, 5, 6);
			tbl.Attach (ppp30 = new RadioButton (ppp1, "5 x 6"), 0, 1, 6, 7);

			tbl.Attach (repeat = new CheckButton (Catalog.GetString ("Repeat")), 1, 2, 0, 1);
			tbl.Attach (crop_marks = new CheckButton (Catalog.GetString ("Print cut marks")), 1, 2, 1, 2);
//			crop_marks.Toggled += TriggerChanged;

			tbl_frame.Child = tbl;
			right_vb.PackStart (tbl_frame, true, true, 0);
			upper.PackStart (right_vb, true, true, 0);

			this.PackStart (upper, true, true, 0);
			this.PackStart (fullpage = new CheckButton (Catalog.GetString ("Full Page (no margin)")), false, false, 0);
			
			HBox hb = new HBox ();
			// Note for translators: "Zoom" is a Fit Mode
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
