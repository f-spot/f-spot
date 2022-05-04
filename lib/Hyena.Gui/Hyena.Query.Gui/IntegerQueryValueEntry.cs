//
// IntegerQueryValueEntry.cs
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
	public class IntegerQueryValueEntry : QueryValueEntry
	{
		protected SpinButton spin_button;
		protected IntegerQueryValue query_value;

		public IntegerQueryValueEntry () : base ()
		{
			spin_button = new SpinButton (0.0, 1.0, 1.0);
			spin_button.Digits = 0;
			spin_button.WidthChars = 4;
			spin_button.ValueChanged += HandleValueChanged;

			Add (spin_button);
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set {
				spin_button.ValueChanged -= HandleValueChanged;
				query_value = value as IntegerQueryValue;
				spin_button.SetRange (query_value.MinValue, query_value.MaxValue);
				spin_button.Value = (double)(query_value.IsEmpty ? query_value.DefaultValue : query_value.IntValue);
				query_value.SetValue (spin_button.ValueAsInt);
				spin_button.ValueChanged += HandleValueChanged;
			}
		}

		protected void HandleValueChanged (object o, EventArgs args)
		{
			query_value.SetValue (spin_button.ValueAsInt);
		}
	}
}
