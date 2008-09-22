/*
 * FSpot.DbItem.cs
 *
 * Author(s):
 *	Larry Ewing
 *	Stephane Delcroix
 *
 * This is free software. See COPYING for details.
 */

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

	public class DbItemEventArgs {
		private DbItem [] items;

		public DbItem [] Items {
			get { return items; }
		}

		public DbItemEventArgs (DbItem [] items)
		{
			this.items = items;
		}

		public DbItemEventArgs (DbItem item)
		{
			this.items = new DbItem [] { item };
		}
	}
}
