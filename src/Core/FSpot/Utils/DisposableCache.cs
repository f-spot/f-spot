// DisposableCache.cs
//
// Author:
//	Stephen Shaw <sshaw@decriptor.com>
//	Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (c) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Utils
{
	public class DisposableCache<TKey, TValue> : Cache<TKey, TValue>, IDisposable
	{
		bool disposed;

		public DisposableCache ()
		{
		}

		public DisposableCache (int maxCount) : base (maxCount)
		{
		}

		public override void Clear ()
		{
			lock (o) {
				foreach (TValue value in Hash.Values) {
					var iDisposable = value as IDisposable;
					iDisposable?.Dispose ();
				}
				mru.Clear ();
				Hash.Clear ();
			}
		}

		public override void Add (TKey key, TValue value)
		{
			lock (o) {
				if (mru.Contains (key))
					mru.Remove (key);

				mru.Insert (0, key);
				Hash [key] = value;

				while (mru.Count >= MaxCount) {
					var iDisposable = Hash [mru [MaxCount - 1]] as IDisposable;
					iDisposable?.Dispose ();
					Hash.Remove (mru [MaxCount - 1]);
					mru.RemoveAt (MaxCount - 1);
				}	
			}
		}

		public override void Remove (TKey key)
		{
			lock (o) {
				var iDisposable = Hash [key] as IDisposable;
				iDisposable?.Dispose ();
				mru.Remove (key);
				Hash.Remove (key);
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				Clear ();
			}
		}
	}
}
