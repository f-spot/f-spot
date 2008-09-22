/*
 * DateCommand.cs
 *
 * Author(s):
 * 	Larry Ewing <lewing@novell.com>
 * 	Bengt Thuree
 *
 * This is free software. See COPYING for details.
 *
 */

using Gtk;
using Gnome;
using System;
using Mono.Unix;
using FSpot;
using FSpot.Query;
using FSpot.UI.Dialog;

public class DateCommands {
	public class Set : GladeDialog {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private DateEdit start_dateedit;
		[Glade.Widget] private DateEdit end_dateedit;
		[Glade.Widget] private ComboBox period_combobox;

		static string [] ranges = {
			"today",
			"yesterday",
			"last7days",
			"last30days",
			"last90days",
			"last360days",
			"currentweek",
			"previousweek",
			"thismonth",
			"previousmonth",
			"thisyear",
			"previousyear",
			"alldates",
			"customizedrange"
		};

		private static string GetString(int index)
		{
			return GetString (ranges [index]);
		}

		private static string GetString(string rangename)
		{
			System.DateTime today = System.DateTime.Today;
			switch (rangename) {
			case "today":
				return Catalog.GetString("Today");
			case "yesterday":
				return Catalog.GetString("Yesterday");
			case "last7days":
				return Catalog.GetString("Last 7 days");
			case "last30days":
				return Catalog.GetString("Last 30 days");
			case "last90days":
				return Catalog.GetString("Last 90 days");
			case "last360days":
				return Catalog.GetString("Last 360 days");
			case "currentweek":
				return Catalog.GetString("Current Week (Mon-Sun)");
			case "previousweek":
				return Catalog.GetString("Previous Week (Mon-Sun)");
			case "thismonth":
				if (today.Year == (today.AddMonths(-1)).Year) // Same year for current and previous month. Present only MONTH
					return today.ToString("MMMM");
				else // Different year for current and previous month. Present both MONTH, and YEAR
					return today.ToString("MMMM, yyyy");
			case "previousmonth":
				if (today.Year == (today.AddMonths(-1)).Year) // Same year for current and previous month. Present only MONTH
					return (today.AddMonths(-1)).ToString("MMMM");
				else // Different year for current and previous month. Present both MONTH, and YEAR
					return (today.AddMonths(-1)).ToString("MMMM, yyyy");
			case "thisyear":
				return today.ToString("yyyy");
			case "previousyear":
				return today.AddYears(-1).ToString("yyyy");
			case "alldates":
				return Catalog.GetString("All Images");
			case "customizedrange":
				return Catalog.GetString("Customized Range");
			default:
				return rangename;
			}	
		}

		private DateRange QueryRange (int index)
		{
			return QueryRange ( ranges [index]);
		}

		private DateRange QueryRange (string rangename)
		{
			System.DateTime today = System.DateTime.Today;
			System.DateTime startdate = today;
			System.DateTime enddate = today;
			bool clear = false;

			switch (rangename) {
			case "today":
				startdate = today;
				enddate = today;
				break;
			case "yesterday":
				startdate = today.AddDays (-1);
				enddate = today.AddDays (-1);
				break;
			case "last7days":
				startdate = today.AddDays (-6);
				enddate = today;
				break;
			case "last30days":
				startdate = today.AddDays (-29);
				enddate = today;
				break;
			case "last90days":
				startdate = today.AddDays (-89);
				enddate = today;
				break;
			case "last360days":
				startdate = today.AddDays (-359);
				enddate = today;
				break;
			case "currentweek":
				startdate = today.AddDays (System.DayOfWeek.Sunday - today.DayOfWeek); // Gets to Sunday
				startdate = startdate.AddDays (1); // Advance to Monday according to ISO 8601
				enddate = today;
				break;
			case "previousweek":
				startdate = today.AddDays (System.DayOfWeek.Sunday - today.DayOfWeek); // Gets to Sunday
				startdate = startdate.AddDays (1); // Advance to Monday according to ISO 8601
				startdate = startdate.AddDays(-7); // Back 7 days
				enddate = startdate.AddDays (6);
				break;
			case "thismonth":
				startdate = new System.DateTime(today.Year, today.Month, 1); // the first of the month
				enddate = today; // we don't have pictures in the future
				break;
			case "previousmonth":
				startdate = new System.DateTime((today.AddMonths(-1)).Year, (today.AddMonths(-1)).Month, 1);
				enddate = new System.DateTime((today.AddMonths(-1)).Year, (today.AddMonths(-1)).Month, System.DateTime.DaysInMonth((today.AddMonths(-1)).Year,(today.AddMonths(-1)).Month));
				break;
			case "thisyear":
				startdate = new System.DateTime(today.Year, 1, 1); // Jan 1st of this year
				enddate = today;
				break;
			case "previousyear":
				startdate = new System.DateTime((today.AddYears(-1)).Year, 1, 1); // Jan 1st of prev year
				enddate = new System.DateTime((today.AddYears(-1)).Year, 12, 31); // Dec, 31 of prev year
				break;
			case "alldates":
				clear = true;
				break;
			case "customizedrange":
				startdate = start_dateedit.Time;
				enddate = end_dateedit.Time;
				break;
			default:
				clear = true;
				break;
			}	
			if (!clear)
				return new DateRange (startdate, enddate.Add (new System.TimeSpan(23,59,59)));
			else
				return null;
		}
	
		void HandleDateEditChanged (object o, EventArgs args)
		{
			period_combobox.Changed -= HandlePeriodComboboxChanged;
           	 	period_combobox.Active = System.Array.IndexOf (ranges, "customizedrange");
			period_combobox.Changed += HandlePeriodComboboxChanged;
		}

		void HandlePeriodComboboxChanged (object o, EventArgs args)
		{
			start_dateedit.DateChanged -= HandleDateEditChanged;
			((Gtk.Entry) start_dateedit.Children [0] as Gtk.Entry).Changed -= HandleDateEditChanged;
			end_dateedit.DateChanged -= HandleDateEditChanged;
			((Gtk.Entry) end_dateedit.Children [0] as Gtk.Entry).Changed -= HandleDateEditChanged;
	
			ComboBox combo = o as ComboBox;
			if (o == null)
				return;

			start_dateedit.Sensitive = (combo.Active != System.Array.IndexOf (ranges, "alldates"));
			end_dateedit.Sensitive = (combo.Active != System.Array.IndexOf (ranges, "alldates"));

			DateRange range = QueryRange (period_combobox.Active);
			if (range != null) {
				start_dateedit.Time = range.Start;
				end_dateedit.Time = range.End;
			}
			
			start_dateedit.DateChanged += HandleDateEditChanged;
			((Gtk.Entry) start_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
			end_dateedit.DateChanged += HandleDateEditChanged;
			((Gtk.Entry) end_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
		}

		public Set (FSpot.PhotoQuery query, Gtk.Window parent_window)
		{
			this.query = query;
			this.parent_window = parent_window;
		}

		public bool Execute ()
		{
			this.CreateDialog ("date_range_dialog");

			// Build the combo box with years and month names
			foreach (string range in ranges)
				period_combobox.AppendText (GetString(range));

			start_dateedit.DateChanged += HandleDateEditChanged;
			((Gtk.Entry) start_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
			end_dateedit.DateChanged += HandleDateEditChanged;
			((Gtk.Entry) end_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
			
			period_combobox.Changed += HandlePeriodComboboxChanged;
           	 	period_combobox.Active = System.Array.IndexOf(ranges, "last7days"); // Default to Last 7 days

			if (query.Range != null) {
				start_dateedit.Time = query.Range.Start;
				end_dateedit.Time = query.Range.End;
			}

			Dialog.TransientFor = parent_window;
			Dialog.DefaultResponse = ResponseType.Ok;
			ResponseType response = (ResponseType) this.Dialog.Run ();

			bool success = false;

			if (response == ResponseType.Ok) {
				query.Range = QueryRange (period_combobox.Active);
				success = true;
			}
			
			this.Dialog.Destroy ();
			return success;
		}
	}
}
