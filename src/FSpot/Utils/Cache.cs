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
					return default(TValue);

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
