//
// FSpot.Widgets.DateEdit.cs: A Date/Time widget with zone support.
//
// Author(s)
//   Stephane Delcroix  <stephane@delcroix.org>
//
// the widgetry to show the calendar popup is ported from the libgnomeui GnomeDateEdit
// widget from Miguel de Icaza, (c) the Free Software Foundation
//
// Copyright (c) 2009 Novell, Inc.
//
// This is free software. See COPYING for details.
//

using System;
using Mono.Unix;
using Gtk;

namespace FSpot.Widgets
{
	public class DateEdit : HBox
	{
		DateEditFlags dateEditFlags;
		DateTimeOffset dateTimeOffset;

#region public API
		public DateEdit () : this (DateTimeOffset.Now)
		{
		}

		public DateEdit (DateTimeOffset dateTimeOffset) : this (dateTimeOffset, DateEditFlags.None)
		{
		}

		public DateEdit (DateTimeOffset dateTimeOffset, DateEditFlags dateEditFlags) : base ()
		{
			this.dateEditFlags = dateEditFlags;
			this.dateTimeOffset = dateTimeOffset;
			CreateWidget ();
		}

		public DateTimeOffset DateTimeOffset {
			get { return dateTimeOffset; }
			set { 
				DateTimeOffset old_dto = dateTimeOffset;
				dateTimeOffset = value; 
				if (dateTimeOffset.Date != old_dto.Date)
					OnDateChanged ();
				if (dateTimeOffset.Offset != old_dto.Offset)
					OnOffsetChanged ();
				if (dateTimeOffset - dateTimeOffset.Date != old_dto - old_dto.Date)
					OnTimeChanged ();
				UpdateWidget ();
			}
		}

		public DateEditFlags DateEditFlags {
			get { return dateEditFlags; }
			set { 
				dateEditFlags = value;
				UpdateWidget ();
			}
		}

		public event EventHandler DateChanged;
		public event EventHandler TimeChanged;
		public event EventHandler OffsetChanged;

		protected void OnDateChanged ()
		{
			EventHandler h = DateChanged;
			if (h != null)
				h (this, EventArgs.Empty);
		}

		protected void OnTimeChanged ()
		{
			EventHandler h = TimeChanged;
			if (h != null)
				h (this, EventArgs.Empty);
		}

		protected void OnOffsetChanged ()
		{
			EventHandler h = OffsetChanged;
			if (h != null)
				h (this, EventArgs.Empty);
		}

		bool ShowSeconds {
			get { return (dateEditFlags & DateEditFlags.ShowSeconds) == DateEditFlags.ShowSeconds; }
		}
#endregion public API

#region Gtk Widgetry
		Entry date_entry;
		Button date_button;
		Entry time_entry;
		Entry offset_entry;
		Calendar calendar;
		Label calendar_label;
		Window calendar_popup;
		Gdk.Color red = new Gdk.Color (255, 0, 0);

		void CreateWidget ()
		{
			Homogeneous = false;
			Spacing = 1;

			Add (date_entry = new Entry () {WidthChars = 10, IsEditable = true});
			date_entry.Activated += HandleDateEntryActivated;
			date_entry.Show ();
			var bbox = new HBox ();
			Widget w;
			bbox.Add (w = calendar_label = new Label (Catalog.GetString ("Calendar")));
			w.Show ();
			bbox.Add (w = new Arrow (ArrowType.Down, ShadowType.Out));
			w.Show ();
			bbox.Show ();
			Add (date_button = new Button (bbox));
			date_button.Clicked += HandleCalendarButtonClicked;
			date_button.Show ();
			Add (time_entry = new Entry () {WidthChars = 12, IsEditable = true});
			time_entry.Activated += HandleTimeEntryActivated;
			time_entry.Show ();
			Add (offset_entry = new Entry () {WidthChars = 6, IsEditable = true});
			offset_entry.Activated += HandleOffsetEntryActivated;
			offset_entry.Show ();

			calendar = new Calendar ();
			calendar.DaySelected += HandleCalendarDaySelected;
			calendar.DaySelectedDoubleClick += HandleCalendarDaySelectedDoubleClick;
			var frame = new Frame ();
			frame.Add (calendar);
			calendar.Show ();
			calendar_popup = new Window (WindowType.Popup) {DestroyWithParent = true, Resizable = false};
			calendar_popup.Add (frame);
			calendar_popup.DeleteEvent += HandlePopupDeleted;
			calendar_popup.KeyPressEvent += HandlePopupKeyPressed;
			calendar_popup.ButtonPressEvent += HandlePopupButtonPressed;
			frame.Show ();

			UpdateWidget ();
		}

		void UpdateWidget ()
		{
			date_entry.Text = dateTimeOffset.ToString ("d");
			date_entry.ModifyBase (StateType.Normal);
			if (ShowSeconds)
				time_entry.Text = dateTimeOffset.ToString ("T");
			else
				time_entry.Text = dateTimeOffset.ToString ("t");
			time_entry.ModifyBase (StateType.Normal);
			time_entry.Visible = (dateEditFlags & DateEditFlags.ShowTime) == DateEditFlags.ShowTime;
			offset_entry.Text = dateTimeOffset.ToString ("zzz");
			offset_entry.ModifyBase (StateType.Normal);
			offset_entry.Visible = (dateEditFlags & DateEditFlags.ShowOffset) == DateEditFlags.ShowOffset;
			calendar_label.Visible = time_entry.Visible || offset_entry.Visible;
		}

		bool GrabPointerAndKeyboard (Gdk.Window window, uint activate_time)
		{
			if (Gdk.Pointer.Grab (window, true,
					      Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask,
					      null, null, activate_time) == Gdk.GrabStatus.Success) {
				if (Gdk.Keyboard.Grab (window, true, activate_time) == Gdk.GrabStatus.Success)
					return true;
				else {
					Gdk.Pointer.Ungrab (activate_time);
					return false;
				}
			}
			return false;
		}

		void PositionPopup ()
		{
			var requisition = calendar_popup.SizeRequest ();
			int x, y;
			date_button.GdkWindow.GetOrigin (out x, out y);
			x += date_button.Allocation.X;
			y += date_button.Allocation.Y;
			x += date_button.Allocation.Width - requisition.Width;
			y += date_button.Allocation.Height;

			if (x < 0)
				x = 0;
			if (y < 0)
				y = 0;
			calendar_popup.Move (x, y);
		}

		void HandleCalendarButtonClicked (object sender, EventArgs e)
		{
			//Temporarily grab pointer and keyboard
			if (!GrabPointerAndKeyboard (GdkWindow, Gtk.Global.CurrentEventTime))
				return;

			//select the day on the calendar

			PositionPopup ();

			Grab.Add (calendar_popup);
			calendar_popup.Show ();
			calendar.GrabFocus ();

			//transfer the grabs to the popup
			GrabPointerAndKeyboard (calendar_popup.GdkWindow, Gtk.Global.CurrentEventTime);
		}

		void HandleDateEntryActivated (object sender, EventArgs e)
		{
			DateTimeOffset new_date;
			if (DateTimeOffset.TryParseExact (date_entry.Text, "d", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowWhiteSpaces, out new_date))
				DateTimeOffset = new DateTimeOffset (calendar.Date + DateTimeOffset.TimeOfDay, DateTimeOffset.Offset);
			else 
				date_entry.ModifyBase (StateType.Normal, red);
		}

		void HandleTimeEntryActivated (object sender, EventArgs e)
		{
			DateTimeOffset new_date;
			if (DateTimeOffset.TryParseExact (String.Format ("{0} {1}", DateTimeOffset.ToString ("d"), time_entry.Text), ShowSeconds ? "G" : "g", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowWhiteSpaces, out new_date)) {
				DateTimeOffset = DateTimeOffset.AddHours (new_date.Hour - DateTimeOffset.Hour).AddMinutes (new_date.Minute - DateTimeOffset.Minute).AddSeconds (new_date.Second - DateTimeOffset.Second);
			} else
				time_entry.ModifyBase (StateType.Normal, red);

		}

		void HandleOffsetEntryActivated (object sender, EventArgs e)
		{
			TimeSpan new_offset;
			if (TimeSpan.TryParse (offset_entry.Text.Trim ('+'), out new_offset))
				DateTimeOffset = new DateTimeOffset (dateTimeOffset.DateTime, new_offset);
			else
				offset_entry.ModifyBase (StateType.Normal, red);
		}

		void HidePopup ()
		{
			calendar_popup.Hide ();
			Grab.Remove (calendar_popup);
		}

		void HandleCalendarDaySelected (object sender, EventArgs e)
		{
			DateTimeOffset = new DateTimeOffset (calendar.Date + DateTimeOffset.TimeOfDay, DateTimeOffset.Offset);
		}

		void HandleCalendarDaySelectedDoubleClick (object sender, EventArgs e)
		{
			HidePopup ();
		}

		void HandlePopupButtonPressed (object sender, ButtonPressEventArgs e)
		{
			var child = Gtk.Global.GetEventWidget (e.Event);
			if (child != calendar_popup) {
				while (child != null) {
					if (child == calendar_popup) {
						e.RetVal = false;
						return;
					}
					child = child.Parent;
				}
			}
			HidePopup ();
			e.RetVal = true;
		}

		void HandlePopupDeleted (object sender, DeleteEventArgs e)
		{
			HidePopup ();
			e.RetVal = false;
		}

		void HandlePopupKeyPressed (object sender, KeyPressEventArgs e)
		{
			if (e.Event.Key != Gdk.Key.Escape) {
				e.RetVal = false;
				return;
			}
			HidePopup ();
			e.RetVal = true;
		}
#endregion

#region Test App
#if DEBUGDATEEDIT
		static void Main ()
		{
			Gtk.Application.Init ();
			Window w = new Window ("test");
			DateEdit de;
			w.Add (de = new DateEdit ());
			de.DateEditFlags |= DateEditFlags.ShowOffset | DateEditFlags.ShowTime | DateEditFlags.ShowSeconds;
			de.Show ();
			w.Show ();
			Gtk.Application.Run ();

		}
#endif
#endregion	
	}
}
