//
// TimeSpanQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Query.Gui
{
	public class TimeSpanQueryValueEntry : QueryValueEntry
	{
		protected SpinButton spin_button;
		protected ComboBox combo;
		protected TimeSpanQueryValue query_value;
		int set_combo = 1;

		protected static readonly TimeFactor[] factors = new TimeFactor[] {
			TimeFactor.Second, TimeFactor.Minute, TimeFactor.Hour, TimeFactor.Day,
			TimeFactor.Week, TimeFactor.Month, TimeFactor.Year
		};

		public TimeSpanQueryValueEntry () : base ()
		{
			spin_button = new SpinButton (0.0, 1.0, 1.0);
			spin_button.Digits = 1;
			spin_button.WidthChars = 4;
			spin_button.SetRange (0.0, double.MaxValue);
			Add (spin_button);

			combo = ComboBox.NewText ();
			combo.AppendText (Strings.Seconds);
			combo.AppendText (Strings.Minutes);
			combo.AppendText (Strings.Hours);
			combo.AppendText (Strings.Days);
			combo.AppendText (Strings.Weeks);
			combo.AppendText (Strings.Months);
			combo.AppendText (Strings.Years);
			combo.Realized += delegate { combo.Active = set_combo; };
			Add (combo);

			spin_button.ValueChanged += HandleValueChanged;
			combo.Changed += HandleValueChanged;
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set {
				spin_button.ValueChanged -= HandleValueChanged;
				combo.Changed -= HandleValueChanged;

				query_value = value as TimeSpanQueryValue;
				spin_button.Value = query_value.FactoredValue;
				combo.Active = set_combo = Array.IndexOf (factors, query_value.Factor);

				spin_button.ValueChanged += HandleValueChanged;
				combo.Changed += HandleValueChanged;
			}
		}

		protected virtual void HandleValueChanged (object o, EventArgs args)
		{
			query_value.SetRelativeValue (spin_button.Value, factors[combo.Active]);
		}
	}
}
