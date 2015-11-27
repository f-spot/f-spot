//
// DateQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Unix;

using Hyena.Query;
using Gtk;

namespace Hyena.Query.Gui
{
    public class DateQueryValueEntry : QueryValueEntry
    {
        protected DateQueryValue query_value;

        protected SpinButton year_entry = new SpinButton (Double.MinValue, Double.MaxValue, 1.0);
        protected SpinButton month_entry = new SpinButton (0.0, 12.0, 1.0);
        protected SpinButton day_entry = new SpinButton (0.0, 31.0, 1.0);

        public DateQueryValueEntry () : base ()
        {
            year_entry.MaxLength = year_entry.WidthChars = 4;
            month_entry.MaxLength = month_entry.WidthChars = 2;
            day_entry.MaxLength = day_entry.WidthChars = 2;
            year_entry.Numeric = month_entry.Numeric = day_entry.Numeric = true;
            month_entry.Wrap = day_entry.Wrap = true;

            year_entry.Value = (double) DateTime.Now.Year;
            month_entry.Value = (double) DateTime.Now.Month;
            day_entry.Value = (double) DateTime.Now.Day;

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
                year_entry.Value = (double) query_value.DateTime.Year;
                month_entry.Value = (double) query_value.DateTime.Month;
                day_entry.Value = (double) query_value.DateTime.Day;

                year_entry.Changed += HandleValueChanged;
                month_entry.Changed += HandleValueChanged;
                day_entry.Changed += HandleValueChanged;
            }
        }

        protected void HandleValueChanged (object o, EventArgs args)
        {
            try {
                DateTime dt = new DateTime (year_entry.ValueAsInt, month_entry.ValueAsInt, day_entry.ValueAsInt);
                query_value.SetValue (dt);
            } catch {
                Log.Debug ("Caught exception raised because of invalid date");
            }
        }
    }
}
