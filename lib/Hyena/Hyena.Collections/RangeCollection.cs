//
// RangeCollection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Collections
{
	public class RangeCollection : ICollection<int>
	{
		public struct Range
		{
			public Range (int start, int end)
			{
				Start = start;
				End = end;
			}

			public override string ToString ()
			{
				return $"{Start}-{End} ({Count})";
			}

			public int Start { get; set; }

			public int End { get; set; }

			public int Count {
				get { return End - Start + 1; }
			}
		}

		const int MIN_CAPACITY = 16;
		Range[] ranges;
		int range_count;
		int index_count;

		public RangeCollection ()
		{
			Clear ();
		}

		#region Private Array Logic

		void Shift (int start, int delta)
		{
			if (delta < 0) {
				start -= delta;
			}

			if (start < range_count) {
				Array.Copy (ranges, start, ranges, start + delta, range_count - start);
			}

			range_count += delta;
		}

		void EnsureCapacity (int growBy)
		{
			int new_capacity = ranges.Length == 0 ? 1 : ranges.Length;
			int min_capacity = ranges.Length == 0 ? MIN_CAPACITY : ranges.Length + growBy;

			while (new_capacity < min_capacity) {
				new_capacity <<= 1;
			}

			Array.Resize (ref ranges, new_capacity);
		}

		void Insert (int position, Range range)
		{
			if (range_count == ranges.Length) {
				EnsureCapacity (1);
			}

			Shift (position, 1);
			ranges[position] = range;
		}

		void RemoveAt (int position)
		{
			Shift (position, -1);
			Array.Clear (ranges, range_count, 1);
		}

		#endregion

		#region Private Range Logic

		bool RemoveIndexFromRange (int index)
		{
			int range_index = FindRangeIndexForValue (index);
			if (range_index < 0) {
				return false;
			}

			Range range = ranges[range_index];
			if (range.Start == index && range.End == index) {
				RemoveAt (range_index);
			} else if (range.Start == index) {
				ranges[range_index].Start++;
			} else if (range.End == index) {
				ranges[range_index].End--;
			} else {
				var split_range = new Range (index + 1, range.End);
				ranges[range_index].End = index - 1;
				Insert (range_index + 1, split_range);
			}

			index_count--;
			return true;
		}

		void InsertRange (Range range)
		{
			int position = FindInsertionPosition (range);
			bool merged_left = MergeLeft (range, position);
			bool merged_right = MergeRight (range, position);

			if (!merged_left && !merged_right) {
				Insert (position, range);
			} else if (merged_left && merged_right) {
				ranges[position - 1].End = ranges[position].End;
				RemoveAt (position);
			}
		}

		bool MergeLeft (Range range, int position)
		{
			int left = position - 1;
			if (left >= 0 && ranges[left].End + 1 == range.Start) {
				ranges[left].End = range.Start;
				return true;
			}

			return false;
		}

		bool MergeRight (Range range, int position)
		{
			if (position < range_count && ranges[position].Start - 1 == range.End) {
				ranges[position].Start = range.End;
				return true;
			}

			return false;
		}

		static int CompareRanges (Range a, Range b)
		{
			return (a.Start + (a.End - a.Start)).CompareTo (b.Start + (b.End - b.Start));
		}

		int FindInsertionPosition (Range range)
		{
			int min = 0;
			int max = range_count - 1;

			while (min <= max) {
				int mid = min + ((max - min) / 2);
				int cmp = CompareRanges (ranges[mid], range);

				if (cmp == 0) {
					return mid;
				} else if (cmp > 0) {
					if (mid > 0 && CompareRanges (ranges[mid - 1], range) < 0) {
						return mid;
					}

					max = mid - 1;
				} else {
					min = mid + 1;
				}
			}

			return min;
		}

		public int FindRangeIndexForValue (int value)
		{
			int min = 0;
			int max = range_count - 1;

			while (min <= max) {
				int mid = min + ((max - min) / 2);
				Range range = ranges[mid];
				if (value >= range.Start && value <= range.End) {
					return mid;    // In Range
				} else if (value < range.Start) {
					max = mid - 1; // Below Range
				} else {
					min = mid + 1; // Above Range
				}
			}

			return ~min;
		}

		#endregion

		#region Public RangeCollection API

		public Range[] Ranges {
			get {
				var ranges_copy = new Range[range_count];
				Array.Copy (ranges, ranges_copy, range_count);
				return ranges_copy;
			}
		}

		public int RangeCount {
			get { return range_count; }
		}

		public int IndexOf (int value)
		{
			int offset = 0;

			foreach (Range range in ranges) {
				if (value >= range.Start && value <= range.End) {
					return offset + (value - range.Start);
				}

				offset += range.End - range.Start + 1;
			}

			return -1;
		}

		public int this[int index] {
			get {
				for (int i = 0, cuml_count = 0; i < range_count && index >= 0; i++) {
					if (index < (cuml_count += ranges[i].Count)) {
						return ranges[i].End - (cuml_count - index) + 1;
					}
				}

				throw new IndexOutOfRangeException (index.ToString ());
			}
		}

		#endregion

		#region ICollection Implementation

		public bool Add (int value)
		{
			if (!Contains (value)) {
				InsertRange (new Range (value, value));
				index_count++;
				return true;
			}

			return false;
		}

		void ICollection<int>.Add (int value)
		{
			Add (value);
		}

		public bool Remove (int value)
		{
			return RemoveIndexFromRange (value);
		}

		public void Clear ()
		{
			range_count = 0;
			index_count = 0;
			ranges = new Range[MIN_CAPACITY];
		}

		public bool Contains (int value)
		{
			return FindRangeIndexForValue (value) >= 0;
		}

		public void CopyTo (int[] array, int index)
		{
			throw new NotImplementedException ();
		}

		public void CopyTo (Array array, int index)
		{
			throw new NotImplementedException ();
		}

		public int Count {
			get { return index_count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		#endregion

		#region IEnumerable Implementation

		public IEnumerator<int> GetEnumerator ()
		{
			for (int i = 0; i < range_count; i++) {
				for (int j = ranges[i].Start; j <= ranges[i].End; j++) {
					yield return j;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion
	}
}
