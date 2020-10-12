//
// BrowsablePointerChangedEventArgs.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Core
{
	public class BrowsablePointerChangedEventArgs : System.EventArgs
	{
		public IPhoto PreviousItem { get; }
		public int PreviousIndex { get; }
		public IBrowsableItemChanges Changes { get; }

		public BrowsablePointerChangedEventArgs (IPhoto previousItem, int previousIndex, IBrowsableItemChanges changes)
		{
			PreviousItem = previousItem;
			PreviousIndex = previousIndex;
			Changes = changes;
		}
	}
}
