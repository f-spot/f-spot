/*
 * Widgets/DateEdit.cs: A Date/Time widget with zone support.
 *   The DateTime part is merely a port of gnome-dateedit.c
 *
 * Author(s)
 *   Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */
using System;
using Mono.Unix;
using Gtk;

namespace FSpot.Widgets
{
	[System.Flags]
	public enum DateEditFlags {
		ShowTime = 1,
		Two4Hr = 1 << 1,
		WeeksStartsOnMonday = 1 << 2,
		ShowZone = 1 << 3,
	}

	public class DateEdit : Gtk.HBox
	{
		//This class keeps the time in UTC and the offset for the timezone
		private class DateTimeZone {

			public delegate void DateTimeZoneChangedHandler (object o, EventArgs e);
			public event DateTimeZoneChangedHandler Changed;

			public DateTimeZone (System.DateTime datetime)
			{
				UtcTime = datetime.ToUniversalTime ();
				offset = Convert.ToInt32 (datetime.ToString ("zz"));
			}
			
			int year;
			public int Year {
				get { return year; }
				set { 
					if (value < 0)
						return;
					year = value;
					if (Changed != null)
						Changed (this, null);
				}
			}

			int month;
			public int Month {
				get { return month; }
				set { 
					if (value < 1 || value > 12)
						return;
					month = value; 
					if (Changed != null)
						Changed (this, null);
				}
			}

			int day;
			public int Day {
				get { return day; }
				set {
					//FIXME check value
					day = value;
					if (Changed != null)
						Changed (this, null);
				}
			}

			int hour;
			public int Hour {
				get { return hour; }
				set {
					if (value < 0 || value > 23)
						return;

					UtcTime = UtcTime.AddHours (value - offset - hour); 
					if (Changed != null)
						Changed (this, null);
				}
			}

			int minute;
			public int Minute {
				get { return minute; }
				set { 
					if (value < 0 || value > 59)
						return;
					minute = value;
					if (Changed != null)
						Changed (this, null);
				}
			}

			int second;

			//FIXME: some tz have 1/2 hours offsets !
			int offset;
			public int Offset {
				get { return offset; }
				set {
					UtcTime = UtcTime.AddHours (offset - value);
					offset = value;
					if (Changed != null)
						Changed (this, null);
				}

			}

			public System.DateTime UtcTime {
				get { return (new System.DateTime (year, month, day, hour, minute, second)); }
				set {
					year = value.Year;
					month = value.Month;
					day = value.Day;
					hour = value.Hour;
					minute = value.Minute;
					second = value.Second;
					if (Changed != null)
						Changed (this, null);
				}
			}

			public System.DateTime TimeinZone (int zone) {
				return (new System.DateTime (year, month, day, hour, minute, second).AddHours (zone));
			}

			public static string OffsetString (int offset)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				if (offset >= 0)
					sb.Append ("+");
				sb.Append (offset.ToString("00"));
				sb.Append (":00");
				return sb.ToString ();
			}
		}

		DateTimeZone datetime;
		DateEditFlags flags;
		int lower_hour = 7;
		int upper_hour = 19;
		int time_increment = 15;
	
		Gtk.Entry date_entry;
		Gtk.Entry time_entry;
		Gtk.Entry zone_entry;
		Gtk.Button date_button;
		Gtk.TreeStore time_store;
		Gtk.ComboBox time_combo;
		Gtk.ComboBox offset_combo;
		Gtk.Window cal_popup;
		Gtk.Calendar calendar;

		public delegate void TimeChangedHandler (object sender, EventArgs e);
		public event TimeChangedHandler Changed;

		public int LowerHour {
			get { return lower_hour; }
			set { 
				//FIXME: check for range
				//FIXME: redraw the time_popup
				lower_hour = value; 
			}
		}

		public int UpperHour {
			get { return upper_hour; }
			set { 
				//FIXME: check for range
				//FIXME: redraw the time_popup
				upper_hour = value; 
			}
		}

		public int TimeIncrement {
			get { return time_increment; }
			set {
				//FIXME: check for authorized values (divisor of 60)
				time_increment = value;
			}
		}

		public DateEdit () : this (System.DateTime.Now)
		{
		}

		public DateEdit (System.DateTime datetime) : this (datetime, DateEditFlags.ShowTime |
									     DateEditFlags.Two4Hr |
									     DateEditFlags.WeeksStartsOnMonday |
									     DateEditFlags.ShowZone)
		{
		}

		public DateEdit (System.DateTime time, DateEditFlags flags)
		{
			datetime = new DateTimeZone (time);
			datetime.Changed += HandleDateTimeZoneChanged;
			this.flags = flags;

			date_entry = new Gtk.Entry ();
			date_entry.WidthChars = 10;
			date_entry.Changed += HandleDateEntryChanged;
			PackStart (date_entry, true, true, 0);
		
			Gtk.HBox b_box = new Gtk.HBox ();
			b_box.PackStart (new Gtk.Label (Catalog.GetString ("Calendar")), true, true, 0);
			b_box.PackStart (new Gtk.Arrow(Gtk.ArrowType.Down, Gtk.ShadowType.Out), true, false, 0);
			date_button = new Gtk.Button (b_box);
			date_button.Clicked += HandleCalendarButtonClicked;
			PackStart (date_button, false, false, 0);

			calendar = new Gtk.Calendar ();
			calendar.DaySelected += HandleCalendarDaySelected;
			Gtk.Frame frame = new Gtk.Frame ();
			frame.Add (calendar);
			cal_popup = new Gtk.Window (Gtk.WindowType.Popup);
			cal_popup.DestroyWithParent = true;
			cal_popup.Add (frame);
			cal_popup.Shown += HandleCalendarPopupShown;
			cal_popup.GrabNotify += HandlePopupGrabNotify;
			frame.Show ();
			calendar.Show ();

			time_entry = new Gtk.Entry ();
			time_entry.WidthChars = 8;
			time_entry.Changed += HandleTimeEntryChanged;
			PackStart (time_entry, true, true, 0);

			Gtk.CellRendererText timecell = new Gtk.CellRendererText ();
			time_combo = new Gtk.ComboBox ();
			time_store = new Gtk.TreeStore (typeof (string), typeof (int), typeof (int)); 
			time_combo.Model = time_store;
			time_combo.PackStart (timecell, true);
			time_combo.SetCellDataFunc (timecell, new CellLayoutDataFunc (TimeCellFunc));
			time_combo.Realized += FillTimeCombo;
			time_combo.Changed += HandleTimeComboChanged;
			PackStart (time_combo, false, false, 0);

			zone_entry = new Gtk.Entry ();
			zone_entry.IsEditable = false;
			zone_entry.MaxLength = 6;
			zone_entry.WidthChars = 6;
			PackStart (zone_entry, true, true, 0);

			Gtk.CellRendererText offsetcell = new Gtk.CellRendererText ();
			offset_combo = new Gtk.ComboBox ();
			offset_combo.Model = new Gtk.TreeStore (typeof (string), typeof (int));
			offset_combo.PackStart (offsetcell, true);
			offset_combo.SetCellDataFunc (offsetcell, new CellLayoutDataFunc (OffsetCellFunc));
			FillOffsetCombo ();
			offset_combo.Changed += HandleOffsetComboChanged;
			PackStart (offset_combo, false, false, 0);

			Update ();
			ShowAll ();
		}

		public int Offset {
			get { return datetime.Offset; }
		}


		void Update ()
		{
			DateTime time = datetime.TimeinZone (datetime.Offset);	
			date_entry.Text = time.ToShortDateString();	
			time_entry.Text = ((flags & DateEditFlags.Two4Hr) == DateEditFlags.Two4Hr) ? time.ToString("HH:mm", null) : time.ToString("hh:mm tt", null);
			zone_entry.Text = DateTimeZone.OffsetString (datetime.Offset);
		}

		void HandleDateTimeZoneChanged (object o, EventArgs e)
		{
			Update ();
			if (Changed != null)
				Changed (this, null);
		}

		public static explicit operator System.DateTime (DateEdit de)
		{
			return de.datetime.UtcTime;
		}

		private void HandleCalendarButtonClicked (object o, EventArgs e)
		{
			if (cal_popup.Visible)
				HideCalendarPopup ();
			else 
				ShowCalendarPopup ();
		}

		private void ShowCalendarPopup ()
		{
			cal_popup.Show();
			cal_popup.GrabFocus ();
		}

		private void HideCalendarPopup ()
		{
			cal_popup.Hide ();	
		}

		private void HandleCalendarPopupShown (object o, EventArgs e)
		{	
			PositionCalendarPopup ();
		}

		private void PositionCalendarPopup ()
		{
			int x, y;
			Gtk.Requisition req = cal_popup.SizeRequest ();
			GetWidgetPosition(date_button, out x, out y);
			cal_popup.Move (x + date_button.Allocation.Width - req.Width, y + date_button.Allocation.Height);
		}

		private void HandleCalendarDaySelected (object o, EventArgs e)
		{
			datetime.Year = calendar.Date.Year;
			datetime.Month = calendar.Date.Month;
			datetime.Day = calendar.Date.Day;
		}

		private void HandlePopupGrabNotify (object o, GrabNotifyArgs args)
		{
			if (args.WasGrabbed)
				HideCalendarPopup ();
		}

		void TimeCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 0);
			(cell as CellRendererText).Text = name;	
		}

		private void FillTimeCombo (object o, EventArgs e)
		{
			FillTimeCombo ();	
		}

		private void FillTimeCombo ()
		{
			if (lower_hour > upper_hour)
				return;

			time_combo.Changed -= HandleTimeComboChanged;

			int localhour = System.DateTime.Now.Hour;

			TreeIter iter;
			for (int i=lower_hour; i<=upper_hour; i++)
			{
				iter = time_store.AppendValues (TimeLabel (i, 0, ((flags & DateEditFlags.Two4Hr) == DateEditFlags.Two4Hr)), i, 0);
				for (int j = time_increment; j < 60; j += time_increment) {
					time_store.AppendValues (iter, TimeLabel (i, j, ((flags & DateEditFlags.Two4Hr) == DateEditFlags.Two4Hr)), i, j);	
				}
				if (i == localhour)
					time_combo.Active = i - lower_hour;

			}
			if (localhour < lower_hour)
				time_combo.Active = 0;
			if (localhour > upper_hour)
				time_combo.Active = upper_hour - lower_hour;

			time_combo.Changed += HandleTimeComboChanged;

			Update ();
		}

		private void HandleTimeEntryChanged (object o, EventArgs e)
		{
			datetime.Changed -= HandleDateTimeZoneChanged;
			try {
				System.DateTime newtime = System.DateTime.Parse (time_entry.Text);
				datetime.Hour = newtime.Hour;
				datetime.Minute = newtime.Minute;
			} catch (FormatException)
			{}
			datetime.Changed += HandleDateTimeZoneChanged;
			if (Changed != null)
				Changed (this, null);
		}

		private void HandleDateEntryChanged (object o, EventArgs e)
		{
			datetime.Changed -= HandleDateTimeZoneChanged;
			try {
				System.DateTime newtime = System.DateTime.Parse (date_entry.Text);
				datetime.Year = newtime.Year;
				datetime.Month = newtime.Month;
				datetime.Day = newtime.Day;
			} catch (FormatException)
			{}
			datetime.Changed += HandleDateTimeZoneChanged;
			if (Changed != null)
				Changed (this, null);

		}

		private void HandleTimeComboChanged (object o, EventArgs e)
		{
			TreeIter iter;
			if (time_combo.GetActiveIter (out iter)) {
				datetime.Hour = (int) time_store.GetValue (iter, 1);
				datetime.Minute = (int) time_store.GetValue (iter, 2);
			}
		}

		void OffsetCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			(cell as CellRendererText).Text = (string)tree_model.GetValue (iter, 0);
		}

		void FillOffsetCombo ()
		{
			for (int i=-12; i <= 13; i++)
				(offset_combo.Model as TreeStore).AppendValues (DateTimeZone.OffsetString(i),i);

			offset_combo.Changed -= HandleOffsetComboChanged;
			offset_combo.Active = datetime.Offset + 12;
			offset_combo.Changed += HandleOffsetComboChanged;
			
			Update ();
		}

		void HandleOffsetComboChanged (object o, EventArgs e)
		{
			TreeIter iter;
			if (offset_combo.GetActiveIter (out iter)) {
				datetime.Offset = (int) offset_combo.Model.GetValue (iter, 1);
			}
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

		static void GetWidgetPosition(Gtk.Widget widget, out int x, out int y)
		{
		    	widget.GdkWindow.GetOrigin(out x, out y);	
			x += widget.Allocation.X;
			y += widget.Allocation.Y;
		}
	}
}
