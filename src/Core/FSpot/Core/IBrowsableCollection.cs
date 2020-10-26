//
// IBrowsableCollection.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Core
{
	public delegate void IBrowsableCollectionChangedHandler (IBrowsableCollection collection);
	public delegate void IBrowsableCollectionItemsChangedHandler (IBrowsableCollection collection, BrowsableEventArgs args);

	public interface IBrowsableCollection
	{
		IEnumerable<IPhoto> Items { get; }

		int IndexOf (IPhoto item);

		IPhoto this[int index] { get; }

		int Count { get; }

		bool Contains (IPhoto item);

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
