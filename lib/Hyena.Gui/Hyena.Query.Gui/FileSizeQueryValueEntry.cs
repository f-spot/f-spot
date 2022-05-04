//
// FileSizeQueryValueEntry.cs
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
	public class FileSizeQueryValueEntry : QueryValueEntry
	{
		protected SpinButton spin_button;
		protected ComboBox combo;
		protected FileSizeQueryValue query_value;

		protected static readonly FileSizeFactor[] factors = new FileSizeFactor[] {
			FileSizeFactor.None, FileSizeFactor.KB, FileSizeFactor.MB, FileSizeFactor.GB
		};

		bool combo_set = false;

		// Relative: [<|>] [num] [minutes|hours] ago
		// TODO: Absolute: [>|>=|=|<|<=] [date/time]
		public FileSizeQueryValueEntry () : base ()
		{
			spin_button = new SpinButton (0.0, 1.0, 1.0);
			spin_button.Digits = 1;
			spin_button.WidthChars = 4;
			spin_button.SetRange (0.0, double.MaxValue);
			Add (spin_button);

			combo = ComboBox.NewText ();
			combo.AppendText (Strings.Bytes);
			combo.AppendText (Strings.KB);
			combo.AppendText (Strings.MB);
			combo.AppendText (Strings.GB);
			combo.Realized += delegate { if (!combo_set) { combo.Active = 2; } };
			Add (combo);

			spin_button.ValueChanged += HandleValueChanged;
			combo.Changed += HandleValueChanged;
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set {
				spin_button.ValueChanged -= HandleValueChanged;
				combo.Changed -= HandleValueChanged;
				query_value = value as FileSizeQueryValue;
				spin_button.Value = query_value.FactoredValue;
				combo_set = true;
				combo.Active = Array.IndexOf (factors, query_value.Factor);
				spin_button.ValueChanged += HandleValueChanged;
				combo.Changed += HandleValueChanged;
			}
		}

		protected void HandleValueChanged (object o, EventArgs args)
		{
			query_value.SetValue (spin_button.Value, factors[combo.Active]);
		}
	}
}
