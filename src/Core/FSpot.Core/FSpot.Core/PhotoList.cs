/*
 * FSpot.PhotoList.cs
 *
 * Author(s):
 *  Larry Ewing
 *
 * This is free software, See COPYING for details
 */

using System.Collections.Generic;

namespace FSpot.Core
{

    public class PhotoList : IBrowsableCollection
    {
        protected List<IPhoto> list;
        private IPhoto[] cache;

        public PhotoList (IEnumerable<IPhoto> photos)
        {
            list = new List<IPhoto> (photos);
        }

        public PhotoList (params IPhoto[] photos) : this(photos as IEnumerable<IPhoto>)
        {
        }

        public int Count {
            get { return list.Count; }
        }

        public void Clear ()
        {
            list.Clear ();
            Reload ();
        }

        public int Capacity {
            set { list.Capacity = value; }
        }

        public void Add (IPhoto photo)
        {
            list.Add (photo);
            Reload ();
        }

        public void Add (IEnumerable<IPhoto> items)
        {
            list.AddRange (items);
            Reload ();
        }

        public int IndexOf (IPhoto item)
        {
            return list.IndexOf (item);
        }

        public bool Contains (IPhoto item)
        {
            return list.Contains (item);
        }

        public IPhoto this[int index] {
            get { return list[index]; }
            set {
                list[index] = value;
                MarkChanged (index, FullInvalidate.Instance);
            }
        }

        public void Sort (IComparer<IPhoto> compare)
        {
            list.Sort (compare);
            Reload ();
        }

        public void Reload ()
        {
            cache = null;
            if (Changed != null)
                Changed (this);
        }

        public void MarkChanged (int num, IBrowsableItemChanges changes)
        {
            MarkChanged (new BrowsableEventArgs (num, changes));
        }

        public void MarkChanged (BrowsableEventArgs args)
        {
            if (ItemsChanged != null)
                ItemsChanged (this, args);
        }

        public IPhoto[] Items {
            get {
                if (cache == null)
                    cache = list.ToArray ();
                
                return cache;
            }
            set {
                list.Clear ();
                Add (value);
            }
        }

        public event IBrowsableCollectionChangedHandler Changed;
        public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
    }
}
