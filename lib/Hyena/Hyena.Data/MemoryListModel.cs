//
// MemoryListModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Hyena.Collections;

namespace Hyena.Data
{
    public class MemoryListModel<T> : BaseListModel<T>
    {
        private List<T> list;

        public MemoryListModel ()
        {
            list = new List<T> ();
            Selection = new Selection ();
        }

        public override void Clear ()
        {
            lock (list) {
                list.Clear ();
            }

            OnCleared ();
        }

        public override void Reload ()
        {
            OnReloaded ();
        }

        public int IndexOf (T item)
        {
            lock (list) {
                return list.IndexOf (item);
            }
        }

        public void Add (T item)
        {
            lock (list) {
                list.Add (item);
            }
        }

        public void Remove (T item)
        {
            lock (list) {
                list.Remove (item);
            }
        }

        public override T this[int index] {
            get {
                lock (list) {
                    if (list.Count <= index || index < 0) {
                        return default (T);
                    }

                    return list[index];
                }
            }
        }

        public override int Count {
            get {
                lock (list) {
                    return list.Count;
                }
            }
        }
    }
}
