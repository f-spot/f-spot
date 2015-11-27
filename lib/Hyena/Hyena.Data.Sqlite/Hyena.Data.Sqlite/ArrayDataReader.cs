//
// ArrayDataReader.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Hyena;

namespace Hyena.Data.Sqlite
{
    internal class ArrayDataReader : IDataReader
    {
        string sql;
        int rows;
        int row = -1;
        int max_got_row = -1;
        List<object[]> data = new List<object[]> ();

        internal ArrayDataReader (IDataReader reader, string sql)
        {
            if (!reader.Read ())
                return;

            FieldCount = reader.FieldCount;
            FieldNames = reader.FieldNames;

            do {
                var vals = new object[FieldCount];
                for (int i = 0; i < FieldCount; i++) {
                    vals[i] = reader[i];
                }

                data.Add (vals);
                rows++;
            } while (reader.Read ());

            this.sql = sql;
        }

        public void Dispose ()
        {
            if (rows > 1 && max_got_row < (rows - 1) && Log.Debugging) {
                Log.WarningFormat ("Disposing ArrayDataReader that has {0} rows but we only read {1} of them\n{2}", rows, row, sql);
            }
            row = -1;
            max_got_row = -1;
        }

        public int FieldCount { get; private set; }
        public string [] FieldNames { get; private set; }

        public bool Read ()
        {
            row++;
            max_got_row++;
            return row < rows;
        }

        public object this[int i] {
            get {
                max_got_row = Math.Max (i, max_got_row);
                return data[row][i];
            }
        }

        public object this[string columnName] {
            get { return this[GetColumnIndex (columnName)]; }
        }

        public T Get<T> (int i)
        {
            return (T) Get (i, typeof(T));
        }

        public object Get (int i, Type asType)
        {
            return QueryReader.GetAs (this[i], asType);
        }

        public T Get<T> (string columnName)
        {
            return Get<T> (GetColumnIndex (columnName));
        }

        private int GetColumnIndex (string columnName)
        {
            return Array.IndexOf (FieldNames, columnName);
        }
    }
}
