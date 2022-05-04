//
// CollectionExtensions.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

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
			var builder = new StringBuilder ();

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
