//
// LruCache.cs
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
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Collections
{
    public struct CacheEntry<TKey, TValue>
    {
        private TKey key;
        public TKey Key {
            get { return key; }
            set { key = value; }
        }

        private TValue value;
        public TValue Value {
            get { return this.value; }
            set { this.value = value; }
        }

        internal DateTime LastUsed;
        internal int UsedCount;
    }

    public class LruCache<TKey, TValue> : IEnumerable<CacheEntry<TKey, TValue>>
    {
        private Dictionary<TKey, CacheEntry<TKey, TValue>> cache;
        private int max_count;
        private long hits;
        private long misses;
        private double? minimum_hit_ratio;

        public LruCache () : this (1024)
        {
        }

        public LruCache (int maxCount) : this (maxCount, null)
        {
        }

        public LruCache (int maxCount, double? minimumHitRatio)
        {
            max_count = maxCount;
            minimum_hit_ratio = minimumHitRatio;
            cache = new Dictionary<TKey, CacheEntry<TKey, TValue>> ();
        }

        public void Add (TKey key, TValue value)
        {
            lock (cache) {
                CacheEntry<TKey, TValue> entry;
                if (cache.TryGetValue (key, out entry)) {
                    Ref (ref entry);
                    cache[key] = entry;
                    return;
                }

                entry.Key = key;
                entry.Value = value;
                Ref (ref entry);
                cache.Add (key, entry);

                misses++;
                EnsureMinimumHitRatio ();

                if (Count > max_count) {
                    TKey expire = FindOldestEntry ();
                    ExpireItem (cache[expire].Value);
                    cache.Remove (expire);
                }
            }
        }

        public bool Contains (TKey key)
        {
            lock (cache) {
                return cache.ContainsKey (key);
            }
        }

        public void Remove (TKey key)
        {
            lock (cache) {
                if (Contains (key)) {
                    ExpireItem (cache[key].Value);
                    cache.Remove (key);
                }
            }
        }

        public bool TryGetValue (TKey key, out TValue value)
        {
            lock (cache) {
                CacheEntry<TKey, TValue> entry;
                if (cache.TryGetValue (key, out entry)) {
                    value = entry.Value;
                    Ref (ref entry);
                    cache[key] = entry;
                    hits++;
                    return true;
                }

                misses++;
                EnsureMinimumHitRatio ();
                value = default (TValue);
                return false;
            }
        }

        private void EnsureMinimumHitRatio ()
        {
            if (minimum_hit_ratio != null && Count > MaxCount && HitRatio < minimum_hit_ratio) {
                MaxCount = Count;
            }
        }

        private void Ref (ref CacheEntry<TKey, TValue> entry)
        {
            entry.LastUsed = DateTime.Now;
            entry.UsedCount++;
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public IEnumerator<CacheEntry<TKey, TValue>> GetEnumerator ()
        {
            lock (cache) {
                foreach (KeyValuePair<TKey, CacheEntry<TKey, TValue>> item in cache) {
                    yield return item.Value;
                }
            }
        }

        // Ok, this blows. I have no time to implement anything clever or proper here.
        // Using a hashtable generally sucks for this, but it's not bad for a 15 minute
        // hack. max_count will be sufficiently small in our case that this can't be
        // felt anyway. Meh.

        private TKey FindOldestEntry ()
        {
            lock (cache) {
                DateTime oldest = DateTime.Now;
                TKey oldest_key = default (TKey);
                foreach (CacheEntry<TKey, TValue> item in this) {
                    if (item.LastUsed < oldest) {
                        oldest = item.LastUsed;
                        oldest_key = item.Key;
                    }
                }
                return oldest_key;
            }
        }

        protected virtual void ExpireItem (TValue item)
        {
        }

        public int MaxCount {
            get { lock (cache) { return max_count; } }
            set { lock (cache) { max_count = value; } }
        }

        public int Count {
            get { lock (cache) { return cache.Count; } }
        }

        public double? MinimumHitRatio { get { return minimum_hit_ratio; } }

        public long Hits { get { return hits; } }
        public long Misses { get { return misses; } }

        public double HitRatio {
            get {
                if (misses == 0) {
                    return 1.0;
                } else {
                    return ((double)hits) / ((double)(hits + misses));
                }
            }
        }
    }
}
