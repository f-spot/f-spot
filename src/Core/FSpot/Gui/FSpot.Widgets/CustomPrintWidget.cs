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
using FSpot.Settings;

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

		void TriggerChanged (object sender, EventArgs e)
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

			Frame pageFrame = new Frame (Catalog.GetString ("Page Setup"));
			VBox pageBox = new VBox ();
			Label currentSettings = new Label ();
			if (FSpotConfiguration.PageSetup != null)
				currentSettings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			else
				currentSettings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), "...", "...");

			pageBox.PackStart (currentSettings, false, false, 0);
			Button page_setup_btn = new Button (Catalog.GetString ("Set Page Size and Orientation"));
			page_setup_btn.Clicked += delegate {
				this.print_operation.DefaultPageSetup = Print.RunPageSetupDialog (null, print_operation.DefaultPageSetup, this.print_operation.PrintSettings); 
				currentSettings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1), 
								Math.Round (print_operation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			};
			pageBox.PackStart (page_setup_btn, false, false, 0);
			pageFrame.Add (pageBox);
			Attach (pageFrame, 1, 2, 3, 4);

			Frame pppFrame = new Frame (Catalog.GetString ("Photos per page"));
			Table pppTbl = new Table(2, 7, false);

			pppTbl.Attach (ppp1 = new RadioButton ("1"), 0, 1, 1, 2);
			pppTbl.Attach (ppp2 = new RadioButton (ppp1, "2"), 0, 1, 2, 3);
			pppTbl.Attach (ppp4 = new RadioButton (ppp1, "2 x 2"), 0, 1, 3, 4);
			pppTbl.Attach (ppp9 = new RadioButton (ppp1, "3 x 3"), 0, 1, 4, 5);
			pppTbl.Attach (ppp20 = new RadioButton (ppp1, "4 x 5"), 0, 1, 5, 6);
			pppTbl.Attach (ppp30 = new RadioButton (ppp1, "5 x 6"), 0, 1, 6, 7);

			pppTbl.Attach (repeat = new CheckButton (Catalog.GetString ("Repeat")), 1, 2, 2, 3);
			pppTbl.Attach (crop_marks = new CheckButton (Catalog.GetString ("Print cut marks")), 1, 2, 3, 4);
//			crop_marks.Toggled += TriggerChanged;

			pppFrame.Child = pppTbl;
			Attach (pppFrame, 0, 1, 1, 2);

			Frame layoutFrame = new Frame (Catalog.GetString ("Photos layout"));
			VBox layoutVbox = new VBox();
			layoutVbox.PackStart (fullpage = new CheckButton (Catalog.GetString ("Full Page (no margin)")), false, false, 0);
			HBox hb = new HBox ();
			// Note for translators: "Zoom" is a Fit Mode
			hb.PackStart (zoom = new RadioButton (Catalog.GetString ("Zoom")), false, false, 0);
			hb.PackStart (fill = new RadioButton (zoom, Catalog.GetString ("Fill")), false, false, 0);
			hb.PackStart (scaled = new RadioButton (zoom, Catalog.GetString ("Scaled")), false, false, 0);
			zoom.Toggled += TriggerChanged;
			fill.Toggled += TriggerChanged;
			scaled.Toggled += TriggerChanged;
			layoutVbox.PackStart (hb, false, false, 0);
			layoutVbox.PackStart (white_border = new CheckButton (Catalog.GetString ("White borders")), false, false, 0);
			white_border.Toggled += TriggerChanged;

			layoutFrame.Child = layoutVbox;
			Attach (layoutFrame, 1, 2, 1, 2);

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
