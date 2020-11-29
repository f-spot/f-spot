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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Settings;

using Gtk;

using Mono.Unix;

namespace FSpot.Widgets
{
	public class CustomPrintWidget : Table
	{
		public delegate void ChangedHandler (Widget widget);

		public enum FitMode
		{
			Zoom,
			Scaled,
			Fill,
		}

		readonly CheckButton fullpage;
		readonly RadioButton ppp1;
		readonly RadioButton ppp2;
		readonly RadioButton ppp4;
		readonly RadioButton ppp9;
		readonly RadioButton ppp20;
		readonly RadioButton ppp30;
		readonly RadioButton zoom;
		readonly RadioButton fill;
		readonly RadioButton scaled;
		readonly CheckButton repeat;
		readonly CheckButton whiteBorder;
		readonly CheckButton cropMarks;
		readonly CheckButton printTags;
		readonly CheckButton printFilename;
		readonly CheckButton printDate;
		readonly CheckButton printTime;
		readonly CheckButton printComments;
		readonly Entry customText;
		readonly PrintOperation printOperation;

		public event ChangedHandler Changed;

		void TriggerChanged (object sender, EventArgs e)
		{
			Changed?.Invoke (this);
		}

		public bool CropMarks => cropMarks.Active;

		public string PrintLabelFormat {
			get {
				string label_format = "{0}";

				if (printTags.Active)
					label_format += "\t{4}";
				if (printFilename.Active)
					label_format += "\t{1}";
				if (printDate.Active)
					label_format += "\t{2}";
				if (printTime.Active)
					label_format += " {3}";
				if (printComments.Active)
					label_format += "\t{5}";

				return label_format;
			}
		}

		public string CustomText => customText.Text;

		public FitMode Fitmode {
			get {
				if (zoom.Active) return FitMode.Zoom;
				else if (fill.Active) return FitMode.Fill;
				else if (scaled.Active) return FitMode.Scaled;
				else
					throw new Exception ("Something is wrong on this GUI");
			}
		}

		public int PhotosPerPage {
			get {
				if (ppp1.Active) return 1;
				else if (ppp2.Active) return 2;
				else if (ppp4.Active) return 4;
				else if (ppp9.Active) return 9;
				else if (ppp20.Active) return 20;
				else if (ppp30.Active) return 30;
				else
					throw new Exception ("Something is wrong on this GUI");
			}
		}

		public Gtk.Image PreviewImage { get; }

		public bool Repeat => repeat.Active;

		public bool UseFullPage => fullpage.Active;

		public bool WhiteBorders => whiteBorder.Active;

		public CustomPrintWidget (PrintOperation printOperation) : base (2, 4, false)
		{
			this.printOperation = printOperation;

			PreviewImage = new Gtk.Image ();
			Attach (PreviewImage, 0, 2, 0, 1);

			using var page_frame = new Frame (Catalog.GetString ("Page Setup"));
			using var page_box = new VBox ();
			using var current_settings = new Label ();
			if (FSpotConfiguration.PageSetup != null)
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"),
								Math.Round (printOperation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1),
								Math.Round (printOperation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			else
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"), "...", "...");

			page_box.PackStart (current_settings, false, false, 0);
			using var page_setup_btn = new Button (Catalog.GetString ("Set Page Size and Orientation"));
			page_setup_btn.Clicked += delegate {
				this.printOperation.DefaultPageSetup = Print.RunPageSetupDialog (null, printOperation.DefaultPageSetup, this.printOperation.PrintSettings);
				current_settings.Text = string.Format (Catalog.GetString ("Paper Size: {0} x {1} mm"),
								Math.Round (printOperation.DefaultPageSetup.GetPaperWidth (Unit.Mm), 1),
								Math.Round (printOperation.DefaultPageSetup.GetPaperHeight (Unit.Mm), 1));
			};
			page_box.PackStart (page_setup_btn, false, false, 0);
			page_frame.Add (page_box);
			Attach (page_frame, 1, 2, 3, 4);

			using var ppp_frame = new Frame (Catalog.GetString ("Photos per page"));
			var ppp_tbl = new Table (2, 7, false);

			ppp_tbl.Attach (ppp1 = new RadioButton ("1"), 0, 1, 1, 2);
			ppp_tbl.Attach (ppp2 = new RadioButton (ppp1, "2"), 0, 1, 2, 3);
			ppp_tbl.Attach (ppp4 = new RadioButton (ppp1, "2 x 2"), 0, 1, 3, 4);
			ppp_tbl.Attach (ppp9 = new RadioButton (ppp1, "3 x 3"), 0, 1, 4, 5);
			ppp_tbl.Attach (ppp20 = new RadioButton (ppp1, "4 x 5"), 0, 1, 5, 6);
			ppp_tbl.Attach (ppp30 = new RadioButton (ppp1, "5 x 6"), 0, 1, 6, 7);

			ppp_tbl.Attach (repeat = new CheckButton (Catalog.GetString ("Repeat")), 1, 2, 2, 3);
			ppp_tbl.Attach (cropMarks = new CheckButton (Catalog.GetString ("Print cut marks")), 1, 2, 3, 4);
			//			crop_marks.Toggled += TriggerChanged;

			ppp_frame.Child = ppp_tbl;
			Attach (ppp_frame, 0, 1, 1, 2);

			using var layout_frame = new Frame (Catalog.GetString ("Photos layout"));
			var layout_vbox = new VBox ();
			layout_vbox.PackStart (fullpage = new CheckButton (Catalog.GetString ("Full Page (no margin)")), false, false, 0);
			using var hb = new HBox ();
			// Note for translators: "Zoom" is a Fit Mode
			hb.PackStart (zoom = new RadioButton (Catalog.GetString ("Zoom")), false, false, 0);
			hb.PackStart (fill = new RadioButton (zoom, Catalog.GetString ("Fill")), false, false, 0);
			hb.PackStart (scaled = new RadioButton (zoom, Catalog.GetString ("Scaled")), false, false, 0);
			zoom.Toggled += TriggerChanged;
			fill.Toggled += TriggerChanged;
			scaled.Toggled += TriggerChanged;
			layout_vbox.PackStart (hb, false, false, 0);
			layout_vbox.PackStart (whiteBorder = new CheckButton (Catalog.GetString ("White borders")), false, false, 0);
			whiteBorder.Toggled += TriggerChanged;

			layout_frame.Child = layout_vbox;
			Attach (layout_frame, 1, 2, 1, 2);

			using var cmt_frame = new Frame (Catalog.GetString ("Custom Text")) {
				Child = customText = new Entry ()
			};
			Attach (cmt_frame, 1, 2, 2, 3);

			using var detail_frame = new Frame (Catalog.GetString ("Photos infos"));
			var detail_vbox = new VBox ();
			detail_vbox.PackStart (printFilename = new CheckButton (Catalog.GetString ("Print file name")), false, false, 0);
			detail_vbox.PackStart (printDate = new CheckButton (Catalog.GetString ("Print photo date")), false, false, 0);
			detail_vbox.PackStart (printTime = new CheckButton (Catalog.GetString ("Print photo time")), false, false, 0);
			detail_vbox.PackStart (printTags = new CheckButton (Catalog.GetString ("Print photo tags")), false, false, 0);
			detail_vbox.PackStart (printComments = new CheckButton (Catalog.GetString ("Print photo comment")), false, false, 0);
			detail_frame.Child = detail_vbox;
			Attach (detail_frame, 0, 1, 2, 4);

			TriggerChanged (this, null);
		}
	}
}
