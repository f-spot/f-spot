//
// CollectionExtensions.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.Collections
{
    public static class CollectionExtensions
    {
        public static void SortedInsert<T> (List<T> list, T value) where T : IComparable
        {
            if (list.Count == 0 || list[list.Count - 1].CompareTo (value) < 0) {
                list.Add (value);
            } else if (list[0].CompareTo (value) > 0) {
                list.Insert (0, value);
            } else {
                int index = list.BinarySearch (value);
                list.Insert (index < 0 ? ~index : index, value);
            }
        }

        public static string Join<T> (IList<T> list)
        {
            return Join<T> (list, ", ");
        }

        public static string Join<T> (IList<T> list, string separator)
        {
            return Join<T> (list, null, null, separator);
        }

        public static string Join<T> (IList<T> list, string wrapper, string separator)
        {
            return Join<T> (list, wrapper, wrapper, separator);
        }

        public static string Join<T> (IList<T> list, string front, string back, string separator)
        {
            StringBuilder builder = new StringBuilder ();

            for (int i = 0, n = list.Count; i < n; i++) {
                if (front != null) {
                    builder.Append (front);
                }

                builder.Append (list[i]);

                if (back != null) {
                    builder.Append (back);
                }

                if (i < n - 1) {
                    builder.Append (separator);
                }
            }

            return builder.ToString ();
        }
    }
}
