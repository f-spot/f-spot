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

using FSpot.Models;

using Hyena;

namespace FSpot.Database
{
	public abstract class DbStore<T> where T : BaseDbSet
	{
		public event EventHandler<DbItemEventArgs<T>> ItemsAdded;
		public event EventHandler<DbItemEventArgs<T>> ItemsRemoved;
		public event EventHandler<DbItemEventArgs<T>> ItemsChanged;

		protected FSpotContext Context { get; }

		public DbStore ()
		{
			Context = new FSpotContext ();
		}
		protected void EmitAdded (T item)
		{
			EmitAdded (new List<T> { item });
		}

		protected void EmitAdded (List<T> items)
		{
			EmitEvent (ItemsAdded, new DbItemEventArgs<T> (items));
		}

		protected void EmitChanged (T item)
		{
			EmitChanged (new List<T> { item });
		}

		protected void EmitChanged (List<T> items)
		{
			EmitChanged (items, new DbItemEventArgs<T> (items));
		}

		protected void EmitChanged (List<T> items, DbItemEventArgs<T> args)
		{
			EmitEvent (ItemsChanged, args);
		}

		protected void EmitRemoved (T item)
		{
			EmitRemoved (new List<T> { item });
		}

		protected void EmitRemoved (List<T> items)
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

		// FIXME, DBConversion: rename to something like GetItem
		public abstract T Get (Guid id);

		public abstract void Remove (T item);
		// If you have made changes to "obj", you have to invoke Commit() to have the changes
		// saved into the database.
		public abstract void Commit (T item);
	}
}
