/*
 * BrowsablePointerChangedEventArgs.cs
 *
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Core
{
	public class BrowsablePointerChangedEventArgs : System.EventArgs
	{
		IPhoto previous_item;
		public IPhoto PreviousItem {
			get { return previous_item; }
		}

		int previous_index;
		public int PreviousIndex {
			get { return previous_index; }
		}

		IBrowsableItemChanges changes;
		public IBrowsableItemChanges Changes {
			get { return changes; }
		}

		public BrowsablePointerChangedEventArgs (IPhoto previous_item, int previous_index, IBrowsableItemChanges changes) : base ()
		{
			this.previous_item = previous_item;
			this.previous_index = previous_index;
			this.changes = changes;
		}
	}
}
