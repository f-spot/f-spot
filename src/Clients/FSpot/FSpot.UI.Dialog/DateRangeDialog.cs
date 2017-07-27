//
// DateRangeDialog.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Bengt Thuree <bengt@thuree.com>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2007 Bengt Thuree
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

using Gtk;

using Mono.Unix;

using FSpot.Query;
using FSpot.Widgets;

namespace FSpot.UI.Dialog
{
	public class DateRangeDialog : BuilderDialog
	{
		[GtkBeans.Builder.Object] Frame startframe;
		[GtkBeans.Builder.Object] Frame endframe;
		[GtkBeans.Builder.Object] ComboBox period_combobox;

		readonly DateEdit start_dateedit;
		readonly DateEdit end_dateedit;

		TreeStore rangestore;

		static readonly string [] ranges = {
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

		public DateRangeDialog (DateRange query_range, Gtk.Window parent_window) : base ("DateRangeDialog.ui", "date_range_dialog")
		{
			TransientFor = parent_window;
			DefaultResponse = ResponseType.Ok;

			(startframe.Child as Bin).Child = start_dateedit = new DateEdit ();
			start_dateedit.Show ();
			(endframe.Child as Bin).Child = end_dateedit = new DateEdit ();
			end_dateedit.Show ();

			var cell_renderer = new CellRendererText ();

			// Build the combo box with years and month names
			period_combobox.Model = rangestore = new TreeStore (typeof (string));
			period_combobox.PackStart (cell_renderer, true);

			period_combobox.SetCellDataFunc (cell_renderer, new CellLayoutDataFunc (RangeCellFunc));

			foreach (string range in ranges)
				rangestore.AppendValues (GetString(range));

			period_combobox.Changed += HandlePeriodComboboxChanged;
			period_combobox.Active = System.Array.IndexOf(ranges, "last7days"); // Default to Last 7 days

			if (query_range != null) {
				start_dateedit.DateTimeOffset = query_range.Start;
				end_dateedit.DateTimeOffset = query_range.End;
			}

		}

		void RangeCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 0);
			(cell as CellRendererText).Text = name;
		}

		string GetString(string rangename)
		{
			DateTime today = DateTime.Today;
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

		public DateRange Range {
			get { return QueryRange (period_combobox.Active); }
		}

		DateRange QueryRange (int index)
		{
			return QueryRange ( ranges [index]);
		}

		DateRange QueryRange (string rangename)
		{
			DateTime today = DateTime.Today;
			DateTime startdate = today;
			DateTime enddate = today;

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
				startdate = start_dateedit.DateTimeOffset.Date;
				enddate = end_dateedit.DateTimeOffset.Date;
				break;
			default:
				clear = true;
				break;
			}
			if (!clear)
				return new DateRange (startdate, enddate.Add (new System.TimeSpan(23,59,59)));

			return null;
		}

		void HandleDateEditChanged (object o, EventArgs args)
		{
			period_combobox.Changed -= HandlePeriodComboboxChanged;
			period_combobox.Active = Array.IndexOf (ranges, "customizedrange");
			period_combobox.Changed += HandlePeriodComboboxChanged;
		}

		void HandlePeriodComboboxChanged (object o, EventArgs args)
		{
			start_dateedit.DateChanged -= HandleDateEditChanged;
			(start_dateedit.Children [0] as Gtk.Entry).Changed -= HandleDateEditChanged;
			end_dateedit.DateChanged -= HandleDateEditChanged;
			(end_dateedit.Children [0] as Gtk.Entry).Changed -= HandleDateEditChanged;

			ComboBox combo = o as ComboBox;
			if (o == null)
				return;

			start_dateedit.Sensitive = (combo.Active != Array.IndexOf (ranges, "alldates"));
			end_dateedit.Sensitive = (combo.Active != Array.IndexOf (ranges, "alldates"));

			DateRange range = QueryRange (period_combobox.Active);
			if (range != null) {
				start_dateedit.DateTimeOffset = range.Start;
				end_dateedit.DateTimeOffset = range.End;
			}

			start_dateedit.DateChanged += HandleDateEditChanged;
			(start_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
			end_dateedit.DateChanged += HandleDateEditChanged;
			(end_dateedit.Children [0] as Gtk.Entry).Changed += HandleDateEditChanged;
		}
	}
}
