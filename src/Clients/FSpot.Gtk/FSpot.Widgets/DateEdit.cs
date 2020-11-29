//
// DateEdit.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007, 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Unix;

using Gtk;

namespace FSpot.Widgets
{
	public class DateEdit : HBox
	{
		DateEditFlags dateEditFlags;
		DateTimeOffset dateTimeOffset;

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
			get => dateTimeOffset;
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
			get => dateEditFlags;
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
			DateChanged?.Invoke (this, EventArgs.Empty);
		}

		protected void OnTimeChanged ()
		{
			TimeChanged?.Invoke (this, EventArgs.Empty);
		}

		protected void OnOffsetChanged ()
		{
			OffsetChanged?.Invoke (this, EventArgs.Empty);
		}

		bool ShowSeconds => (dateEditFlags & DateEditFlags.ShowSeconds) == DateEditFlags.ShowSeconds;

		#region Gtk Widgetry
		Entry dateEntry;
		Button dateButton;
		Entry timeEntry;
		Entry offsetEntry;
		Calendar calendar;
		Label calendarLabel;
		Window calendarPopup;
		Gdk.Color red = new Gdk.Color (255, 0, 0);

		void CreateWidget ()
		{
			Homogeneous = false;
			Spacing = 1;

			Add (dateEntry = new Entry () { WidthChars = 10, IsEditable = true });
			dateEntry.Changed += HandleDateEntryChanged;
			dateEntry.Show ();
			using var bbox = new HBox ();
			Widget w;
			bbox.Add (w = calendarLabel = new Label (Catalog.GetString ("Calendar")));
			w.Show ();
			bbox.Add (w = new Arrow (ArrowType.Down, ShadowType.Out));
			w.Show ();
			bbox.Show ();
			Add (dateButton = new Button (bbox));
			dateButton.Clicked += HandleCalendarButtonClicked;
			dateButton.Show ();
			Add (timeEntry = new Entry () { WidthChars = 12, IsEditable = true });
			timeEntry.Changed += HandleTimeEntryChanged;
			timeEntry.Show ();
			Add (offsetEntry = new Entry () { WidthChars = 6, IsEditable = true });
			offsetEntry.Changed += HandleOffsetEntryChanged;
			offsetEntry.Show ();

			calendar = new Calendar ();
			calendar.DaySelected += HandleCalendarDaySelected;
			calendar.DaySelectedDoubleClick += HandleCalendarDaySelectedDoubleClick;
			using var frame = new Frame { calendar };
			calendar.Show ();
			calendarPopup = new Window (WindowType.Popup) { DestroyWithParent = true, Resizable = false };
			calendarPopup.Add (frame);
			calendarPopup.DeleteEvent += HandlePopupDeleted;
			calendarPopup.KeyPressEvent += HandlePopupKeyPressed;
			calendarPopup.ButtonPressEvent += HandlePopupButtonPressed;
			frame.Show ();

			UpdateWidget ();
		}

		void UpdateWidget ()
		{
			dateEntry.Text = dateTimeOffset.ToString ("d");
			dateEntry.ModifyBase (StateType.Normal);
			if (ShowSeconds)
				timeEntry.Text = dateTimeOffset.ToString ("T");
			else
				timeEntry.Text = dateTimeOffset.ToString ("t");
			timeEntry.ModifyBase (StateType.Normal);
			timeEntry.Visible = (dateEditFlags & DateEditFlags.ShowTime) == DateEditFlags.ShowTime;
			offsetEntry.Text = dateTimeOffset.ToString ("zzz");
			offsetEntry.ModifyBase (StateType.Normal);
			offsetEntry.Visible = (dateEditFlags & DateEditFlags.ShowOffset) == DateEditFlags.ShowOffset;
			calendarLabel.Visible = timeEntry.Visible || offsetEntry.Visible;
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
			var requisition = calendarPopup.SizeRequest ();
			dateButton.GdkWindow.GetOrigin (out var x, out var y);
			x += dateButton.Allocation.X;
			y += dateButton.Allocation.Y;
			x += dateButton.Allocation.Width - requisition.Width;
			y += dateButton.Allocation.Height;

			if (x < 0)
				x = 0;
			if (y < 0)
				y = 0;
			calendarPopup.Move (x, y);
		}

		void HandleCalendarButtonClicked (object sender, EventArgs e)
		{
			//Temporarily grab pointer and keyboard
			if (!GrabPointerAndKeyboard (GdkWindow, Gtk.Global.CurrentEventTime))
				return;

			//select the day on the calendar

			PositionPopup ();

			Grab.Add (calendarPopup);
			calendarPopup.Show ();
			calendar.GrabFocus ();

			//transfer the grabs to the popup
			GrabPointerAndKeyboard (calendarPopup.GdkWindow, Gtk.Global.CurrentEventTime);
		}

		void HandleDateEntryChanged (object sender, EventArgs e)
		{
			if (DateTimeOffset.TryParseExact (dateEntry.Text, "d", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var new_date))
				DateTimeOffset = new DateTimeOffset (new_date.Date + DateTimeOffset.TimeOfDay, DateTimeOffset.Offset);
			else
				dateEntry.ModifyBase (StateType.Normal, red);
		}

		void HandleTimeEntryChanged (object sender, EventArgs e)
		{
			if (DateTimeOffset.TryParseExact (string.Format ("{0} {1}", DateTimeOffset.ToString ("d"), timeEntry.Text), ShowSeconds ? "G" : "g", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var new_date)) {
				DateTimeOffset = DateTimeOffset.AddHours (new_date.Hour - DateTimeOffset.Hour).AddMinutes (new_date.Minute - DateTimeOffset.Minute).AddSeconds (new_date.Second - DateTimeOffset.Second);
			} else
				timeEntry.ModifyBase (StateType.Normal, red);

		}

		void HandleOffsetEntryChanged (object sender, EventArgs e)
		{
			if (TimeSpan.TryParse (offsetEntry.Text.Trim ('+'), out var new_offset))
				DateTimeOffset = new DateTimeOffset (dateTimeOffset.DateTime, new_offset);
			else
				offsetEntry.ModifyBase (StateType.Normal, red);
		}

		void HidePopup ()
		{
			calendarPopup.Hide ();
			Grab.Remove (calendarPopup);
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
			if (child != calendarPopup) {
				while (child != null) {
					if (child == calendarPopup) {
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
