//
// DbStore.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

using Hyena;

namespace FSpot.Database
{
	public abstract class DbStore<T> where T : DbItem
	{
		// DbItem cache.

		public event EventHandler<DbItemEventArgs<T>> ItemsAdded;
		public event EventHandler<DbItemEventArgs<T>> ItemsRemoved;
		public event EventHandler<DbItemEventArgs<T>> ItemsChanged;

		protected Dictionary<uint, object> itemCache;
		readonly bool cacheIsImmortal;

		protected void AddToCache (T item)
		{
			if (itemCache.ContainsKey (item.Id)) {
				itemCache.Remove (item.Id);
			}

			if (cacheIsImmortal) {
				itemCache.Add (item.Id, item);
			} else {
				itemCache.Add (item.Id, new WeakReference (item));
			}
		}

		protected T LookupInCache (uint id)
		{
			if (!itemCache.ContainsKey (id)) {
				return null;
			}

			if (cacheIsImmortal) {
				return itemCache[id] as T;
			}

			var weakref = itemCache[id] as WeakReference;
			return (T)weakref.Target;
		}

		protected void RemoveFromCache (T item)
		{
			itemCache.Remove (item.Id);
		}

		protected void EmitAdded (T item)
		{
			EmitAdded (new T[] { item });
		}

		protected void EmitAdded (T[] items)
		{
			EmitEvent (ItemsAdded, new DbItemEventArgs<T> (items));
		}

		protected void EmitChanged (T item)
		{
			EmitChanged (new T[] { item });
		}

		protected void EmitChanged (T[] items)
		{
			EmitChanged (items, new DbItemEventArgs<T> (items));
		}

		protected void EmitChanged (T[] items, DbItemEventArgs<T> args)
		{
			EmitEvent (ItemsChanged, args);
		}

		protected void EmitRemoved (T item)
		{
			EmitRemoved (new T[] { item });
		}

		protected void EmitRemoved (T[] items)
		{
			EmitEvent (ItemsRemoved, new DbItemEventArgs<T> (items));
		}

		void EmitEvent (EventHandler<DbItemEventArgs<T>> evnt, DbItemEventArgs<T> args)
		{
			if (evnt == null) {
				// No subscribers.
				return;
			}

			ThreadAssist.ProxyToMain (() => {
				evnt (this, args);
			});
		}

		public bool CacheEmpty {
			get { return itemCache.Count == 0; }
		}

		protected IDb Db { get; private set; }
		protected FSpotDatabaseConnection Database { get { return Db.Database; } }

		// Constructor.

		public DbStore (IDb db, bool cache_is_immortal)
		{
			Db = db;
			cacheIsImmortal = cache_is_immortal;

			itemCache = new Dictionary<uint, object> ();
		}


		// Abstract methods.

		public abstract T Get (uint id);

		public abstract void Remove (T item);
		// If you have made changes to "obj", you have to invoke Commit() to have the changes
		// saved into the database.
		public abstract void Commit (T item);
	}
}
