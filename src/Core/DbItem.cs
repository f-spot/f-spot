/*
 * FSpot.DbItem.cs
 *
 * Author(s):
 *	Larry Ewing
 *	Stephane Delcroix
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot
{
	public class DbItem {
		uint id;
		public uint Id {
			get { return id; }
		}
	
		protected DbItem (uint id) {
			this.id = id;
		}
	}

	public class DbItemEventArgs<T> : EventArgs where T : DbItem {
		private T [] items;

		public T [] Items {
			get { return items; }
		}

		public DbItemEventArgs (T [] items) : base ()
		{
			this.items = items;
		}

		public DbItemEventArgs (T item) : base ()
		{
			this.items = new T [] { item };
		}
	}
}
