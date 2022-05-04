//
// DictionaryModelCache.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Hyena.Data
{
	public abstract class DictionaryModelCache<T> : ModelCache<T> where T : ICacheableItem, new()
	{
		protected Dictionary<long, T> cache;

		public DictionaryModelCache (ICacheableModel model) : base (model)
		{
			cache = new Dictionary<long, T> (model.FetchCount);
		}

		public override bool ContainsKey (long i)
		{
			return cache.ContainsKey (i);
		}

		public override void Add (long i, T item)
		{
			cache.Add (i, item);
		}

		public override T this[long i] {
			get { return cache[i]; }
		}

		public override void Clear ()
		{
			cache.Clear ();
		}
	}
}
