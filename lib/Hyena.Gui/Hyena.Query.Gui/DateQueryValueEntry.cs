//
// DateQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Query.Gui
{
	public class DateQueryValueEntry : QueryValueEntry
	{
		protected DateQueryValue query_value;

		protected SpinButton year_entry = new SpinButton (double.MinValue, double.MaxValue, 1.0);
		protected SpinButton month_entry = new SpinButton (0.0, 12.0, 1.0);
		protected SpinButton day_entry = new SpinButton (0.0, 31.0, 1.0);

		public DateQueryValueEntry () : base ()
		{
			year_entry.MaxLength = year_entry.WidthChars = 4;
			month_entry.MaxLength = month_entry.WidthChars = 2;
			day_entry.MaxLength = day_entry.WidthChars = 2;
			year_entry.Numeric = month_entry.Numeric = day_entry.Numeric = true;
			month_entry.Wrap = day_entry.Wrap = true;

			year_entry.Value = (double)DateTime.Now.Year;
			month_entry.Value = (double)DateTime.Now.Month;
			day_entry.Value = (double)DateTime.Now.Day;

			year_entry.Changed += HandleValueChanged;
			month_entry.Changed += HandleValueChanged;
			day_entry.Changed += HandleValueChanged;

			Add (year_entry);
			Add (new Label ("-"));
			Add (month_entry);
			Add (new Label ("-"));
			Add (day_entry);
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set {
				year_entry.Changed -= HandleValueChanged;
				month_entry.Changed -= HandleValueChanged;
				day_entry.Changed -= HandleValueChanged;

				query_value = value as DateQueryValue;
				year_entry.Value = (double)query_value.DateTime.Year;
				month_entry.Value = (double)query_value.DateTime.Month;
				day_entry.Value = (double)query_value.DateTime.Day;

				year_entry.Changed += HandleValueChanged;
				month_entry.Changed += HandleValueChanged;
				day_entry.Changed += HandleValueChanged;
			}
		}

		protected void HandleValueChanged (object o, EventArgs args)
		{
			try {
				var dt = new DateTime (year_entry.ValueAsInt, month_entry.ValueAsInt, day_entry.ValueAsInt);
				query_value.SetValue (dt);
			} catch {
				Log.Debug ("Caught exception raised because of invalid date");
			}
		}
	}
}
