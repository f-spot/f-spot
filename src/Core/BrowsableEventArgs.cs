/*
 * FSpot.BrowsableEventArgs.cs
 * 
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public class BrowsableEventArgs : System.EventArgs {
		int [] items;

		public int [] Items {
			get { return items; }
		}

		public BrowsableEventArgs (int num)
		{
			items = new int [] { num };
		}

		public BrowsableEventArgs (int [] items)
		{
			this.items = items;
		}
	}
}
