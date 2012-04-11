//
// BrowsableCollectionProxy.cs
//
// Author:
//   Paul Wellner Bou <paul@purecodes.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Wellner Bou
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;

namespace FSpot.Core {
    public class BrowsableCollectionProxy : IBrowsableCollection {

        private IBrowsableCollection collection;

        public IBrowsableCollection Collection {
            get { return collection; }
            set {
                if (collection == value)
                    return;

                if (collection != null) {
                    collection.Changed -= ChangedHandler;
                    collection.ItemsChanged -= ItemsChangedHandler;
                }

                collection = value;

                if (collection != null) {
                    collection.Changed += ChangedHandler;
                    collection.ItemsChanged += ItemsChangedHandler;
                }

                ChangedHandler (this);
            }
        }

        public int Count {
            get { return collection != null ? collection.Count : 0; }
        }

        public int IndexOf (IPhoto item)
        {
            if (collection == null)
                return -1;
            return collection.IndexOf (item);
        }

        public bool Contains (IPhoto item)
        {
            if (collection == null)
                return false;
            return collection.Contains (item);
        }

        public IPhoto this [int index] {
            get {
                if (collection == null)
                    throw new System.IndexOutOfRangeException ();
                return collection [index];
            }
        }

        public void MarkChanged (int num, IBrowsableItemChanges changes)
        {
            if (collection != null)
                collection.MarkChanged (num, changes);
        }

        public IPhoto [] Items {
            get {
                return collection.Items;
            }
        }

        protected virtual void ChangedHandler (IBrowsableCollection collection)
        {
            if (Changed != null)
                Changed (this);
        }

        protected virtual void ItemsChangedHandler (IBrowsableCollection collection, BrowsableEventArgs args)
        {
            if (ItemsChanged != null)
                ItemsChanged (this, args);
        }

        public event IBrowsableCollectionChangedHandler Changed;
        public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
    }
}
