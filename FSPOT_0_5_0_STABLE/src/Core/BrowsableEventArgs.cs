/*
 * FSpot.BrowsableEventArgs.cs
 * 
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot
{
	public class BrowsableEventArgs : System.EventArgs {
		private readonly int [] items;
		public int [] Items {
			get { return items; }
		}

		IBrowsableItemChanges changes;
		public IBrowsableItemChanges Changes {
			get { return changes; }
		}

		public BrowsableEventArgs (int item, IBrowsableItemChanges changes) : this (new int[] {item}, changes)
		{
		}

		public BrowsableEventArgs (int[] items, IBrowsableItemChanges changes)
		{
			this.items = items;
			this.changes = changes;
		}
	}
}
