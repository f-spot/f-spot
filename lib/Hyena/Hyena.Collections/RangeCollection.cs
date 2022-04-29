//
// RangeCollection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Collections
{
    public

    class RangeCollection : ICloneable, ICollection<int>
    {
        public struct Range
        {
            private int start;
            private int end;

            public Range (int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public override string ToString ()
            {
                return string.Format ("{0}-{1} ({2})", Start, End, Count);
            }

            public int Start {
                get { return start; }
                set { start = value; }
            }

            public int End {
                get { return end; }
                set { end = value; }
            }

            public int Count {
                get { return End - Start + 1; }
            }
        }

        private const int MIN_CAPACITY = 16;
        private Range [] ranges;
        private int range_count;
        private int index_count;

        public RangeCollection ()
        {
            Clear ();
        }

#region Private Array Logic

        private void Shift (int start, int delta)
        {
            if (delta < 0) {
                start -= delta;
            }

            if (start < range_count) {
                Array.Copy (ranges, start, ranges, start + delta, range_count - start);
            }

            range_count += delta;
        }

        private void EnsureCapacity (int growBy)
        {
            int new_capacity = ranges.Length == 0 ? 1 : ranges.Length;
            int min_capacity = ranges.Length == 0 ? MIN_CAPACITY : ranges.Length + growBy;

            while (new_capacity < min_capacity) {
                new_capacity <<= 1;
            }

            Array.Resize (ref ranges, new_capacity);
        }

        private void Insert (int position, Range range)
        {
            if (range_count == ranges.Length) {
                EnsureCapacity (1);
            }

            Shift (position, 1);
            ranges[position] = range;
        }

        private void RemoveAt (int position)
        {
            Shift (position, -1);
            Array.Clear (ranges, range_count, 1);
        }

#endregion

#region Private Range Logic

        private bool RemoveIndexFromRange (int index)
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
                Range split_range = new Range (index + 1, range.End);
                ranges[range_index].End = index - 1;
                Insert (range_index + 1, split_range);
            }

            index_count--;
            return true;
        }

        private void InsertRange (Range range)
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

        private bool MergeLeft (Range range, int position)
        {
            int left = position - 1;
            if (left >= 0 && ranges[left].End + 1 == range.Start) {
                ranges[left].End = range.Start;
                return true;
            }

            return false;
        }

        private bool MergeRight (Range range, int position)
        {
            if (position < range_count && ranges[position].Start - 1 == range.End) {
                ranges[position].Start = range.End;
                return true;
            }

            return false;
        }

        private static int CompareRanges (Range a, Range b)
        {
            return (a.Start + (a.End - a.Start)).CompareTo (b.Start + (b.End - b.Start));
        }

        private int FindInsertionPosition (Range range)
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

        public Range [] Ranges {
            get {
                Range [] ranges_copy = new Range[range_count];
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

        public void CopyTo (int [] array, int index)
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

#region ICloneable Implementation

        public object Clone ()
        {
            return MemberwiseClone ();
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
