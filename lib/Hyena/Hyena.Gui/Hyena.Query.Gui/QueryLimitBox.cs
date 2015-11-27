//
// QueryLimitBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Text;

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryLimitBox : HBox
    {
        private CheckButton enabled_checkbox;
        private SpinButton count_spin;
        private ComboBox limit_combo;
        private ComboBox order_combo;

        private QueryOrder [] orders;
        private QueryLimit [] limits;

        public QueryLimitBox (QueryOrder [] orders, QueryLimit [] limits) : base ()
        {
            this.orders = orders;
            this.limits = limits;

            Spacing = 5;

            enabled_checkbox = new CheckButton (Catalog.GetString ("_Limit to"));
            enabled_checkbox.Toggled += OnEnabledToggled;

            count_spin = new SpinButton (0, Double.MaxValue, 1);
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
                    order_combo.AppendText (String.Empty);
                } else {
                    order_combo.AppendText (order.Label);
                }
            }

            PackStart (enabled_checkbox, false, false, 0);
            PackStart (count_spin, false, false, 0);
            PackStart (limit_combo, false, false, 0);
            PackStart (new Label (Catalog.GetString ("selected by")), false, false, 0);
            PackStart (order_combo, false, false, 0);

            enabled_checkbox.Active = false;
            limit_combo.Active = 0;
            order_combo.Active = 0;

            OnEnabledToggled (null, null);

            ShowAll ();
        }

        private bool IsRowSeparator (TreeModel model, TreeIter iter)
        {
            return String.IsNullOrEmpty (model.GetValue (iter, 0) as string);
        }

        public QueryLimit Limit {
            get { return Enabled ? limits [limit_combo.Active] : null; }
            set {
                if (value != null)
                    limit_combo.Active = Array.IndexOf (limits, value);
            }
        }

        public IntegerQueryValue LimitValue {
            get {
                if (!Enabled)
                    return null;

                IntegerQueryValue val = new IntegerQueryValue ();
                val.SetValue (count_spin.ValueAsInt);
                return val;
            }

            set {
                if (value != null && !value.IsEmpty)
                    count_spin.Value = value.IntValue;
            }
        }

        public QueryOrder Order {
            get { return Enabled ? orders [order_combo.Active] : null; }
            set {
                if (value != null) {
                    order_combo.Active = Array.IndexOf (orders, value);
                }
            }
        }

        private void OnEnabledToggled (object o, EventArgs args)
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
