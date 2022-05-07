//
// BrowsableEventArgs.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Core
{
	public class BrowsableEventArgs : System.EventArgs
	{
		public List<int> Items { get; }

		public IBrowsableItemChanges Changes { get; }

		public BrowsableEventArgs (int item, IBrowsableItemChanges changes) : this (new List<int> { item }, changes)
		{
		}

		public BrowsableEventArgs (List<int> items, IBrowsableItemChanges changes)
		{
			Items = items;
			Changes = changes;
		}
	}
}
