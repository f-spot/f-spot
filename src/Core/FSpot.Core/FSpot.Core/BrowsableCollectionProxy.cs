/*
 * FSpot.BrowsableCollectionProxy.cs
 *
 * Author(s):
 *    Paul Wellner Bou
 *
 * This is free software, See COPYING for details
 */

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

        public int IndexOf (IBrowsableItem item)
        {
            if (collection == null)
                return -1;
            return collection.IndexOf (item);
        }

        public bool Contains (IBrowsableItem item)
        {
            if (collection == null)
                return false;
            return collection.Contains (item);
        }

        public IBrowsableItem this [int index] {
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

        public IBrowsableItem [] Items {
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
