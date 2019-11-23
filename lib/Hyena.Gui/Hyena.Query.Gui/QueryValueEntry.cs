//
// QueryValueEntry.cs
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
using System.Collections.Generic;

using Hyena.Query;
using Gtk;

namespace Hyena.Query.Gui
{
    public abstract class QueryValueEntry : HBox
    {
        private static Dictionary<Type, Type> types = new Dictionary<Type, Type> ();

        protected int DefaultWidth {
            get { return 170; }
        }

        public QueryValueEntry () : base ()
        {
            Spacing = 5;
        }

        public abstract QueryValue QueryValue { get; set; }

        public static QueryValueEntry Create (QueryValue qv)
        {
            Type qv_type = qv.GetType ();
            Type entry_type = null;

            foreach (KeyValuePair<Type, Type> pair in types) {
                if (pair.Value == qv_type) {
                    entry_type = pair.Key;
                    break;
                }
            }

            // If we don't have an entry type that's exactly for our type, take a more generic one
            if (entry_type == null) {
                foreach (KeyValuePair<Type, Type> pair in types) {
                    if (qv_type.IsSubclassOf (pair.Value)) {
                        entry_type = pair.Key;
                        break;
                    }
                }
            }

            if (entry_type != null) {
                QueryValueEntry entry = Activator.CreateInstance (entry_type) as QueryValueEntry;
                entry.QueryValue = qv;
                return entry;
            }

            return null;
        }

        public static void AddSubType (Type entry_type, Type query_value_type)
        {
            types[entry_type] = query_value_type;
        }

        public static Type GetValueType (QueryValueEntry entry)
        {
            return types [entry.GetType ()];
        }

        static QueryValueEntry () {
            AddSubType (typeof(StringQueryValueEntry), typeof(StringQueryValue));
            AddSubType (typeof(IntegerQueryValueEntry), typeof(IntegerQueryValue));
            AddSubType (typeof(DateQueryValueEntry), typeof(DateQueryValue));
            AddSubType (typeof(FileSizeQueryValueEntry), typeof(FileSizeQueryValue));
            AddSubType (typeof(TimeSpanQueryValueEntry), typeof(TimeSpanQueryValue));
            AddSubType (typeof(RelativeTimeSpanQueryValueEntry), typeof(RelativeTimeSpanQueryValue));
            AddSubType (typeof(NullQueryValueEntry), typeof(NullQueryValue));
        }
    }
}
