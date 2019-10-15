//  DisposableCache.cs
//
//  Author:
//		Stephen Shaw <sshaw@decriptor.com>
//		Stephane Delcroix <stephane@delcroix.org>
//
//	Copyright (c) 2013 Stephen Shaw
//	Copyright (C) 2008 Novell, Inc.
//	Copyright (C) 2008 Stephane Delcroix
//
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

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
