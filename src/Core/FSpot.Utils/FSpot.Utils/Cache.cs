//
// Cache.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
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

using System;
using System.Collections.Generic;

using Hyena;

namespace FSpot.Utils
{
	public class Cache<TKey, TValue>
	{
		private const int DEFAULT_CACHE_SIZE = 10;
		protected int max_count;
		protected Dictionary<TKey, TValue> hash;
		protected List<TKey> mru;
		protected object o = new object ();

		public Cache () : this (DEFAULT_CACHE_SIZE)
		{
		}

		public Cache (int max_count)
		{
			this.max_count = max_count;
			hash = new Dictionary<TKey, TValue> (max_count + 1);
			mru = new List<TKey> (max_count + 1);
		}

		public virtual void Add (TKey key, TValue value)
		{
			lock (o) {
				if (mru.Contains (key))
					mru.Remove (key);

				mru.Insert (0, key);
				hash [key] = value;
				
				while (mru.Count >= max_count) {
					hash.Remove (mru [max_count - 1]);
					mru.RemoveAt (max_count - 1);
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

				return hash [key];
			}
		}

		public virtual void Clear ()
		{
			lock (o) {
				mru.Clear ();
				hash.Clear ();
			}
		}

		public virtual void Remove (TKey key)
		{
			lock (o) {
				mru.Remove (key);
				hash.Remove (key);
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
			return mru.Contains (key);
		}
	}

	public class DisposableCache<TKey, TValue> : Cache<TKey, TValue>, IDisposable
	{
		public DisposableCache (int max_count) : base (max_count)
		{
		}

		public override void Clear ()
		{
			lock (o) {
				foreach (TValue value in hash.Values)
					if (value is IDisposable)
						(value as IDisposable).Dispose ();
				mru.Clear ();
				hash.Clear ();
			}
		}

		public override void Add (TKey key, TValue value)
		{
			lock (o) {
				if (mru.Contains (key))
					mru.Remove (key);

				mru.Insert (0, key);
				hash [key] = value;
				
				while (mru.Count >= max_count) {
					if (hash [mru [max_count - 1]] is IDisposable)
						(hash [mru [max_count - 1]] as IDisposable).Dispose ();
					hash.Remove (mru [max_count - 1]);
					mru.RemoveAt (max_count - 1);
				}	
			}
		}

		public override void Remove (TKey key)
		{
			lock (o) {
				if (hash [key] is IDisposable)
					(hash [key] as IDisposable).Dispose ();
				mru.Remove (key);
				hash.Remove (key);
			}
		}

		public void Dispose ()
		{
			Clear ();
			GC.SuppressFinalize (this);
		}

		~DisposableCache ()
		{
			Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
			Clear ();	
		}
	}
}

