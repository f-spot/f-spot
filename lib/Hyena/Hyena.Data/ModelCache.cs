//
// ModelCache.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data
{
	public abstract class ModelCache<T> where T : ICacheableItem, new()
	{
		ICacheableModel model;
		protected ICacheableModel Model { get { return model; } }

		public ModelCache (ICacheableModel model)
		{
			this.model = model;
		}

		public virtual T GetValue (long index)
		{
			lock (this) {
				if (ContainsKey (index))
					return this[index];

				FetchSet (index, model.FetchCount);

				if (ContainsKey (index))
					return this[index];

				return default;
			}
		}

		// Responsible for fetching a set of items and placing them in the cache
		protected abstract void FetchSet (long offset, long limit);

		// Regenerate the cache
		public abstract void Reload ();

		public abstract bool ContainsKey (long i);
		public abstract void Add (long i, T item);
		public abstract T this[long i] { get; }
		public abstract void Clear ();
	}
}
