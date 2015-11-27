//
// StringQueryValueEntry.cs
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
    public class StringQueryValueEntry : QueryValueEntry
    {
        protected Gtk.Entry entry;
        protected StringQueryValue query_value;

        public StringQueryValueEntry () : base ()
        {
            entry = new Entry ();
            entry.WidthRequest = DefaultWidth;
            entry.Changed += HandleChanged;
            Add (entry);
        }

        public override QueryValue QueryValue {
            get { return query_value; }
            set {
                entry.Changed -= HandleChanged;
                query_value = value as StringQueryValue;
                entry.Text = (query_value.Value as string) ?? String.Empty;
                entry.Changed += HandleChanged;
            }
        }

        protected void HandleChanged (object o, EventArgs args)
        {
            query_value.ParseUserQuery (entry.Text);
        }
    }
}
