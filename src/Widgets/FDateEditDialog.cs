/*
 * FSpot.Widgets.DateEditDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2008 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;

namespace FSpot.Widgets
{
	internal class DateEditDialog : VBox
	{
		public DateTimeOffset DateTimeOffset {get; set;}
		public event EventHandler DateChanged;

		Calendar calendar;
		TreeStore time_store;
		ComboBox time_combo;

		public DateEditDialog (DateTimeOffset dto)
		{
			DateTimeOffset = dto;

			calendar = new Calendar ();
			calendar.Date = DateTimeOffset.Date;
			calendar.DaySelected += HandleDateChanged;
			calendar.Show ();
			Add (calendar);

			HBox timebox = new HBox ();

			Entry timeentry = new Entry ();
			timeentry.Show ();
			timebox.Add (timeentry);

			Gtk.CellRendererText timecell = new Gtk.CellRendererText ();
			time_combo = new Gtk.ComboBox ();
			time_store = new Gtk.TreeStore (typeof (string), typeof (int), typeof (int));
			time_combo.Model = time_store;
			time_combo.PackStart (timecell, true);
			time_combo.SetCellDataFunc (timecell, new CellLayoutDataFunc (TimeCellFunc));
			time_combo.Realized += FillTimeCombo;
			time_combo.Changed += HandleTimeComboChanged;
			time_combo.Show ();
			timebox.Add (time_combo);

			timebox.Show ();
			Add (timebox);
		}

		void TimeCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 0);
			(cell as CellRendererText).Text = name;
		}

		void FillTimeCombo (object o, EventArgs e)
		{
			FillTimeCombo ();
		}

		void FillTimeCombo ()
		{
			int lower_hour = 0;
			int upper_hour = 23;
			int time_increment = 10;

			if (lower_hour > upper_hour)
				return;

			time_combo.Changed -= HandleTimeComboChanged;

			int localhour = System.DateTime.Now.Hour;

			TreeIter iter;
			for (int i=lower_hour; i<=upper_hour; i++)
			{
				iter = time_store.AppendValues (TimeLabel (i, 0, true), i, 0);
				for (int j = time_increment; j < 60; j += time_increment) {
					time_store.AppendValues (iter, TimeLabel (i, j, true), i, j);
				}
				if (i == localhour)
					time_combo.Active = i - lower_hour;

			}
			if (localhour < lower_hour)
				time_combo.Active = 0;
			if (localhour > upper_hour)
				time_combo.Active = upper_hour - lower_hour;

			time_combo.Changed += HandleTimeComboChanged;

			//Update ();
		}

		void HandleTimeComboChanged (object o, EventArgs e)
		{
			TreeIter iter;
			if (time_combo.GetActiveIter (out iter)) {
//				datetime.Hour = (int) time_store.GetValue (iter, 1);
//				datetime.Minute = (int) time_store.GetValue (iter, 2);
			}
		}


		void HandleDateChanged (object o, EventArgs args)
		{
			Console.WriteLine ("DateChanged: {0}", calendar.Date);
			if (DateChanged != null)
				DateChanged (o, args);
		}

		private static string TimeLabel (int h, int m, bool two4hr)
		{
			if (two4hr) {
				return String.Format ("{0}{1}{2}",
							h % 24,
							System.Globalization.DateTimeFormatInfo.CurrentInfo.TimeSeparator,
							m.ToString ("00"));
			} else {
				return String.Format ("{0}{1}{2} {3}",
							(h + 11) % 12 + 1,
							System.Globalization.DateTimeFormatInfo.CurrentInfo.TimeSeparator,
							m.ToString ("00"),
							(12 <= h && h < 24) ?
								System.Globalization.DateTimeFormatInfo.CurrentInfo.PMDesignator :
								System.Globalization.DateTimeFormatInfo.CurrentInfo.AMDesignator);
			}
		}


		static void Main ()
		{
			Application.Init ();
			Window w = new Window ("test");
			w.Add (new DateEditDialog (DateTimeOffset.Now));
			w.ShowAll ();
			Application.Run ();
		}
	}

}
