//
// QueryLimitBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Query.Gui
{
	public class QueryLimitBox : HBox
	{
		CheckButton enabled_checkbox;
		SpinButton count_spin;
		ComboBox limit_combo;
		ComboBox order_combo;

		QueryOrder[] orders;
		QueryLimit[] limits;

		public QueryLimitBox (QueryOrder[] orders, QueryLimit[] limits) : base ()
		{
			this.orders = orders;
			this.limits = limits;

			Spacing = 5;

			enabled_checkbox = new CheckButton (Strings.LimitToMnemonic);
			enabled_checkbox.Toggled += OnEnabledToggled;

			count_spin = new SpinButton (0, double.MaxValue, 1);
			count_spin.Numeric = true;
			count_spin.Digits = 0;
			count_spin.Value = 25;
			count_spin.SetSizeRequest (60, -1);

			limit_combo = ComboBox.NewText ();
			foreach (QueryLimit limit in limits) {
				limit_combo.AppendText (limit.Label);
			}

			order_combo = ComboBox.NewText ();
			order_combo.RowSeparatorFunc = IsRowSeparator;
			foreach (QueryOrder order in orders) {
				if (order == null) {
					order_combo.AppendText (string.Empty);
				} else {
					order_combo.AppendText (order.Label);
				}
			}

			PackStart (enabled_checkbox, false, false, 0);
			PackStart (count_spin, false, false, 0);
			PackStart (limit_combo, false, false, 0);
			PackStart (new Label (Strings.SelectedBy), false, false, 0);
			PackStart (order_combo, false, false, 0);

			enabled_checkbox.Active = false;
			limit_combo.Active = 0;
			order_combo.Active = 0;

			OnEnabledToggled (null, null);

			ShowAll ();
		}

		bool IsRowSeparator (TreeModel model, TreeIter iter)
		{
			return string.IsNullOrEmpty (model.GetValue (iter, 0) as string);
		}

		public QueryLimit Limit {
			get { return Enabled ? limits[limit_combo.Active] : null; }
			set {
				if (value != null)
					limit_combo.Active = Array.IndexOf (limits, value);
			}
		}

		public IntegerQueryValue LimitValue {
			get {
				if (!Enabled)
					return null;

				var val = new IntegerQueryValue ();
				val.SetValue (count_spin.ValueAsInt);
				return val;
			}

			set {
				if (value != null && !value.IsEmpty)
					count_spin.Value = value.IntValue;
			}
		}

		public QueryOrder Order {
			get { return Enabled ? orders[order_combo.Active] : null; }
			set {
				if (value != null) {
					order_combo.Active = Array.IndexOf (orders, value);
				}
			}
		}

		void OnEnabledToggled (object o, EventArgs args)
		{
			count_spin.Sensitive = enabled_checkbox.Active;
			limit_combo.Sensitive = enabled_checkbox.Active;
			order_combo.Sensitive = enabled_checkbox.Active;
		}

		public bool Enabled {
			get { return enabled_checkbox.Active; }
			set { enabled_checkbox.Active = value; }
		}
	}
}
