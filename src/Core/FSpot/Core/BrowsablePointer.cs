//
// BrowsablePointer.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2005-2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Models;

namespace FSpot.Core
{
	public class BrowsablePointer
	{
		readonly IBrowsableCollection browsableCollection;
		IPhoto item;
		int index;
		public event EventHandler<BrowsablePointerChangedEventArgs> Changed;

		public BrowsablePointer (IBrowsableCollection collection, int index)
		{
			browsableCollection = collection ?? throw new ArgumentNullException (nameof (collection));
			Index = index;
			item = Current;

			collection.Changed += HandleCollectionChanged;
			collection.ItemsChanged += HandleCollectionItemsChanged;
		}

		public IBrowsableCollection Collection => browsableCollection;

		public IPhoto Current {
			get {
				if (!IsValid)
					return null;

				return browsableCollection[index];
			}
		}

		bool Valid (int val)
		{
			return val >= 0 && val < browsableCollection.Count;
		}

		public bool IsValid => Valid (Index);

		public void MoveFirst ()
		{
			Index = 0;
		}

		public void MoveLast ()
		{
			Index = browsableCollection.Count - 1;
		}

		public void MoveNext ()
		{
			MoveNext (false);
		}

		public void MoveNext (bool wrap)
		{
			int val = Index;

			val++;
			if (!Valid (val))
				val = wrap ? 0 : Index;

			Index = val;
		}

		public void MovePrevious ()
		{
			MovePrevious (false);
		}

		public void MovePrevious (bool wrap)
		{
			int val = Index;

			val--;
			if (!Valid (val))
				val = wrap ? browsableCollection.Count - 1 : Index;

			Index = val;
		}

		public int Index {
			get { return index; }
			set {
				if (index != value) {
					SetIndex (value);
				}
			}
		}

		void SetIndex (int value, IBrowsableItemChanges changes = null)
		{
			var args = new BrowsablePointerChangedEventArgs (Current, index, changes);

			index = value;
			item = Current;

			Changed?.Invoke (this, args);
		}

		protected void HandleCollectionItemsChanged (IBrowsableCollection browsableCollection, BrowsableEventArgs eventArgs)
		{
			foreach (var eventItem in eventArgs.Items) {
				if (eventItem == Index)
					SetIndex (Index, eventArgs.Changes);
			}
		}

		protected void HandleCollectionChanged (IBrowsableCollection browsableCollection)
		{
			if (browsableCollection == null)
				throw new ArgumentNullException (nameof (browsableCollection));
			int old_location = Index;
			int next_location = browsableCollection.IndexOf (item);

			if (old_location == next_location) {
				if (!Valid (next_location))
					SetIndex (0, null);

				return;
			}

			if (Valid (next_location))
				SetIndex (next_location);
			else if (Valid (old_location))
				SetIndex (old_location);
			else if (Valid (old_location - 1))
				SetIndex (old_location - 1);
			else
				SetIndex (0);
		}
	}
}
