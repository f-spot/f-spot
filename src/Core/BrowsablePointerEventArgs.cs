/*
 * FSpot.BrowsablePointerEventArgs.cs
 *
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public class BrowsablePointerChangedArgs {
		IBrowsableItem previous_item;
		public IBrowsableItem PreviousItem {
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

		public BrowsablePointerChangedArgs (IBrowsableItem previous_item, int previous_index, IBrowsableItemChanges changes)
		{
			this.previous_item = previous_item;
			this.previous_index = previous_index;
			this.changes = changes;
		}
	}
}
