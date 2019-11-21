//
// FileSizeQueryValueEntry.cs
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
    public class FileSizeQueryValueEntry : QueryValueEntry
    {
        protected SpinButton spin_button;
        protected ComboBox combo;
        protected FileSizeQueryValue query_value;

        protected static readonly FileSizeFactor [] factors = new FileSizeFactor [] {
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
            spin_button.SetRange (0.0, Double.MaxValue);
            Add (spin_button);

            combo = ComboBox.NewText ();
            combo.AppendText (Catalog.GetString ("bytes"));
            combo.AppendText (Catalog.GetString ("KB"));
            combo.AppendText (Catalog.GetString ("MB"));
            combo.AppendText (Catalog.GetString ("GB"));
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
            query_value.SetValue (spin_button.Value, factors [combo.Active]);
        }
    }
}
