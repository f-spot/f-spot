/*
 * IBrowsableCollection.cs
 *
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Core
{
	public delegate void IBrowsableCollectionChangedHandler (IBrowsableCollection collection);
	public delegate void IBrowsableCollectionItemsChangedHandler (IBrowsableCollection collection, BrowsableEventArgs args);

	public interface IBrowsableCollection {
		// FIXME this should really be ToArray ()
		IBrowsableItem [] Items {
			get;
		}

		int IndexOf (IBrowsableItem item);

		IBrowsableItem this [int index] {
			get;
		}

		int Count {
			get;
		}

		bool Contains (IBrowsableItem item);

		// FIXME the Changed event needs to pass along information
		// about the items that actually changed if possible.  For things like
		// TrayView everything has to be redrawn when a single
		// item has been added or removed which adds too much
		// overhead.
		event IBrowsableCollectionChangedHandler Changed;
		event IBrowsableCollectionItemsChangedHandler ItemsChanged;

		void MarkChanged (int index, IBrowsableItemChanges changes);
	}
}
