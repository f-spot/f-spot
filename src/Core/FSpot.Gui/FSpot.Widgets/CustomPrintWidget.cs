//
// CustomPrintWidget.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Vincent Pomey <vpomey@free.fr>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008-2009 Stephane Delcroix
// Copyright (C) 2009 Vincent Pomey
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

using System;

using Mono.Unix;

using Gtk;

namespace FSpot.Widgets
{
	public class CustomPrintWidget : Table
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

		CheckButton repeat, white_border, crop_marks, print_tags,
			print_filename, print_date, print_time, print_comments;
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

		public string PrintLabelFormat {
			get {
				string label_format = "{0}";

				if (print_tags.Active)
					label_format += "\t{4}";
				if (print_filename.Active)
					label_format += "\t{1}";
				if (print_date.Active)
					label_format += "\t{2}";
				if (print_time.Active)
					label_format += " {3}";
				if (print_comments.Active)
					label_format += "\t{5}";

				return label_format;
			}
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

		public CustomPrintWidget (PrintOperation print_operation) : base (2, 4, false)
		{
			this.print_operation = print_operation;

			preview_image = new Gtk.Image ();
			Attach (preview_image, 0, 2, 0, 1);

			Frame page_frame = new Frame (Catalog.GetString ("Page Setup"));
			VBox page_box = new VBox ();
			Label current_settings = new Label ();
			if (FSpot.Settings.Global.PageSetup != null)
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			else
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), "...", "...");

			page_box.PackStart (current_settings, false, false, 0);
			Button page_setup_btn = new Button (Catalog.GetString ("Set Page Size and Orientation"));
			page_setup_btn.Clicked += delegate {
				this.print_operation.DefaultPageSetup = Print.RunPageSetupDialog (null, print_operation.DefaultPageSetup, this.print_operation.PrintSettings); 
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			};
			page_box.PackStart (page_setup_btn, false, false, 0);
			page_frame.Add (page_box);
			Attach (page_frame, 1, 2, 3, 4);

			Frame ppp_frame = new Frame (Catalog.GetString ("Photos per page"));
			Table ppp_tbl = new Table(2, 7, false);

			ppp_tbl.Attach (ppp1 = new RadioButton ("1"), 0, 1, 1, 2);
			ppp_tbl.Attach (ppp2 = new RadioButton (ppp1, "2"), 0, 1, 2, 3);
			ppp_tbl.Attach (ppp4 = new RadioButton (ppp1, "2 x 2"), 0, 1, 3, 4);
			ppp_tbl.Attach (ppp9 = new RadioButton (ppp1, "3 x 3"), 0, 1, 4, 5);
			ppp_tbl.Attach (ppp20 = new RadioButton (ppp1, "4 x 5"), 0, 1, 5, 6);
			ppp_tbl.Attach (ppp30 = new RadioButton (ppp1, "5 x 6"), 0, 1, 6, 7);

			ppp_tbl.Attach (repeat = new CheckButton (Catalog.GetString ("Repeat")), 1, 2, 2, 3);
			ppp_tbl.Attach (crop_marks = new CheckButton (Catalog.GetString ("Print cut marks")), 1, 2, 3, 4);
//			crop_marks.Toggled += TriggerChanged;

			ppp_frame.Child = ppp_tbl;
			Attach (ppp_frame, 0, 1, 1, 2);

			Frame layout_frame = new Frame (Catalog.GetString ("Photos layout"));
			VBox layout_vbox = new VBox();
			layout_vbox.PackStart (fullpage = new CheckButton (Catalog.GetString ("Full Page (no margin)")), false, false, 0);
			HBox hb = new HBox ();
			// Note for translators: "Zoom" is a Fit Mode
			hb.PackStart (zoom = new RadioButton (Catalog.GetString ("Zoom")), false, false, 0);
			hb.PackStart (fill = new RadioButton (zoom, Catalog.GetString ("Fill")), false, false, 0);
			hb.PackStart (scaled = new RadioButton (zoom, Catalog.GetString ("Scaled")), false, false, 0);
			zoom.Toggled += TriggerChanged;
			fill.Toggled += TriggerChanged;
			scaled.Toggled += TriggerChanged;
			layout_vbox.PackStart (hb, false, false, 0);
			layout_vbox.PackStart (white_border = new CheckButton (Catalog.GetString ("White borders")), false, false, 0);
			white_border.Toggled += TriggerChanged;

			layout_frame.Child = layout_vbox;
			Attach (layout_frame, 1, 2, 1, 2);

			Frame cmt_frame = new Frame (Catalog.GetString ("Custom Text"));
			cmt_frame.Child = custom_text = new Entry ();
			Attach (cmt_frame, 1, 2, 2, 3);

			Frame detail_frame = new Frame (Catalog.GetString ("Photos infos"));
			VBox detail_vbox = new VBox();
			detail_vbox.PackStart (print_filename = new CheckButton (Catalog.GetString ("Print file name")), false, false, 0);
			detail_vbox.PackStart (print_date = new CheckButton (Catalog.GetString ("Print photo date")), false, false, 0);
			detail_vbox.PackStart (print_time = new CheckButton (Catalog.GetString ("Print photo time")), false, false, 0);
			detail_vbox.PackStart (print_tags = new CheckButton (Catalog.GetString ("Print photo tags")), false, false, 0);
			detail_vbox.PackStart (print_comments = new CheckButton (Catalog.GetString ("Print photo comment")), false, false, 0);
			detail_frame.Child = detail_vbox;
			Attach (detail_frame, 0, 1, 2, 4);

			TriggerChanged (this, null);
		}
	}
}
