//
// DbStore.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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

		protected DbStore ()
		{
			Context = new FSpotContext ();
		}

		protected void EmitAdded (T item)
		{
			EmitAdded (new[] { item });
		}

		protected void EmitAdded (T[] items)
		{
			EmitEvent (ItemsAdded, new DbItemEventArgs<T> (items));
		}

		protected void EmitChanged (T item)
		{
			EmitChanged (new[] { item });
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
			EmitRemoved (new[] { item });
		}

		protected void EmitRemoved (T[] items)
		{
			EmitEvent (ItemsRemoved, new DbItemEventArgs<T> (items));
		}

		void EmitEvent (EventHandler<DbItemEventArgs<T>> evnt, DbItemEventArgs<T> args)
		{
			// No subscribers.
			if (evnt == null)
				return;

			ThreadAssist.ProxyToMain (() => {
				evnt (this, args);
			});
		}

		// FIXME, rename to something like GetItem
		public abstract T Get (Guid id);

		public abstract void Remove (T item);

		// If you have made changes to "obj", you have to invoke Commit() to have the changes
		// saved into the database.
		public abstract void Commit (T item);
	}
}
