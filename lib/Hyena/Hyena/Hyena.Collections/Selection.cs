//
// Selection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Alex Launi <alex.launi@canonical.com>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2010 Alex Launi
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

    class Selection : IEnumerable<int>
    {
        RangeCollection ranges = new RangeCollection ();
        private int max_index;
        private int first_selected_index;

        public event EventHandler Changed;
        public event EventHandler FocusChanged;
        private int focused_index = -1;

        public Selection ()
        {
        }

        public int FocusedIndex {
            get { return focused_index; }
            set {
                focused_index = value;
                var handler = FocusChanged;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            }
        }

        public void Notify ()
        {
            OnChanged ();
        }

        protected virtual void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void ToggleSelect (int index)
        {
            if (!ranges.Remove (index)) {
                ranges.Add (index);
            }

            OnChanged ();
        }

        public void Select (int index, bool notify)
        {
            ranges.Add (index);
            if (Count == 1) {
                first_selected_index = index;
            }

            if (notify) {
                OnChanged ();
            }
        }

        public void Select (int index)
        {
            Select (index, true);
        }

        public void QuietSelect (int index)
        {
            ranges.Add (index);
            if (Count == 1)
                first_selected_index = index;
        }

        public void Unselect (int index)
        {
            if (ranges.Remove (index))
                OnChanged ();
        }

        public void QuietUnselect (int index)
        {
            ranges.Remove (index);
        }

        public bool Contains(int index)
        {
            return ranges.Contains (index);
        }

        public void SelectFromFirst (int end, bool clear, bool notify)
        {
            bool contains = Contains (first_selected_index);

            if (clear)
                Clear (false);

            if (contains)
                SelectRange (first_selected_index, end, notify);
            else
                Select (end, notify);
        }

        public void SelectFromFirst (int end, bool clear)
        {
            SelectFromFirst (end, clear, true);
        }

        public void SelectRange (int a, int b, bool notify)
        {
            int start = Math.Min (a, b);
            int end = Math.Max (a, b);

            int i;
            for (i = start; i <= end; i++) {
                ranges.Add (i);
            }

            if (Count == i)
                first_selected_index = a;

            if (notify) {
                OnChanged ();
            }
        }

        public void SelectRange (int a, int b)
        {
            SelectRange (a, b, true);
        }

        public void UnselectRange (int a, int b, bool notify)
        {
            int start = Math.Min (a, b);
            int end = Math.Max (a, b);

            int i;
            for (i = start; i <= end; i++) {
                ranges.Remove (i);
            }

            if (notify) {
                OnChanged ();
            }
        }

        public void UnselectRange (int a, int b)
        {
            UnselectRange (a, b, true);
        }

        public virtual void SelectAll ()
        {
            SelectRange (0, max_index);
        }

        public void Clear ()
        {
            Clear (true);
        }

        public void Clear (bool raise)
        {
            if (ranges.Count <= 0) {
                return;
            }

            ranges.Clear ();
            if (raise)
                OnChanged ();
        }

        public int Count {
            get { return ranges.Count; }
        }

        public int MaxIndex {
            set { max_index = value; }
            get { return max_index; }
        }

        public virtual bool AllSelected {
            get {
                if (ranges.RangeCount == 1) {
                    RangeCollection.Range range = ranges.Ranges[0];
                    return range.Start == 0 && range.End == max_index;
                }

                return false;
            }
        }

        public RangeCollection RangeCollection {
            get { return ranges; }
        }

        public RangeCollection.Range [] Ranges {
            get { return ranges.Ranges; }
        }

        public int FirstIndex {
            get { return Count > 0 ? ranges[0] : -1; }
        }

        public int LastIndex {
            get { return Count > 0 ? ranges[Count - 1]: -1; }
        }

        public IEnumerator<int> GetEnumerator ()
        {
            return ranges.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public override string ToString ()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            sb.AppendFormat ("<Selection Count={0}", Count);
            foreach (RangeCollection.Range range in Ranges) {
                sb.AppendFormat (" ({0}, {1})", range.Start, range.End);
            }
            sb.Append (">");
            return sb.ToString ();
        }
    }
}
