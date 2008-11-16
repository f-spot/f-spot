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

	public class DbItemEventArgs : EventArgs {
		private DbItem [] items;

		public DbItem [] Items {
			get { return items; }
		}

		public DbItemEventArgs (DbItem [] items) : base ()
		{
			this.items = items;
		}

		public DbItemEventArgs (DbItem item) : base ()
		{
			this.items = new DbItem [] { item };
		}
	}
}
