//
// ArrayModelCache.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public abstract class ArrayModelCache<T> : ModelCache<T> where T : ICacheableItem, new()
	{
		protected T[] cache;
		protected long offset = -1;
		protected long limit = 0;

		public ArrayModelCache (ICacheableModel model) : base (model)
		{
			cache = new T[Model.FetchCount];
		}

		public override bool ContainsKey (long i)
		{
			return (i >= offset &&
					i <= (offset + limit));
		}

		public override void Add (long i, T item)
		{
			if (cache.Length != Model.FetchCount) {
				cache = new T[Model.FetchCount];
				Clear ();
			}

			if (offset == -1 || i < offset || i >= (offset + cache.Length)) {
				offset = i;
				limit = 0;
			}

			cache[i - offset] = item;
			limit++;
		}

		public override T this[long i] {
			get { return cache[i - offset]; }
		}

		public override void Clear ()
		{
			offset = -1;
			limit = 0;
		}
	}
}
