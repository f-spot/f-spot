//
// IntegerQueryValueEntry.cs
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

using Hyena.Query;
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
                spin_button.Value = (double) (query_value.IsEmpty ? query_value.DefaultValue : query_value.IntValue);
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
