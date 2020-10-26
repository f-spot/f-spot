//
// Cache.cs
//
// Author:
//	 Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (c) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Utils
{
	public class Cache<TKey, TValue>
	{
		const int DEFAULT_CACHE_SIZE = 10;
		protected int MaxCount;
		protected Dictionary<TKey, TValue> Hash;
		protected List<TKey> mru;
		protected object o = new object ();

		public Cache (int maxCount = DEFAULT_CACHE_SIZE)
		{
			MaxCount = maxCount;
			Hash = new Dictionary<TKey, TValue> (maxCount + 1);
			mru = new List<TKey> (maxCount + 1);
		}

		public virtual void Add (TKey key, TValue value)
		{
			lock (o) {
				if (mru.Contains (key))
					mru.Remove (key);

				mru.Insert (0, key);
				Hash [key] = value;
				
				while (mru.Count >= MaxCount) {
					Hash.Remove (mru [MaxCount - 1]);
					mru.RemoveAt (MaxCount - 1);
				}	
			}
		}

		public TValue Get (TKey key)
		{
			lock (o) {
				if (!mru.Contains (key))
					return default;

				mru.Remove (key);
				mru.Insert (0, key);

				return Hash [key];
			}
		}

		public virtual void Clear ()
		{
			lock (o) {
				mru.Clear ();
				Hash.Clear ();
			}
		}

		public virtual void Remove (TKey key)
		{
			lock (o) {
				mru.Remove (key);
				Hash.Remove (key);
			}
		}

		public bool TryRemove (TKey key)
		{
			try {
				Remove (key);
				return true;
			} catch (KeyNotFoundException) {
				return false;
			}
		}

		public bool Contains (TKey key)
		{
			lock (o)
			{
				return mru.Contains (key);
			}
		}
	}
}
