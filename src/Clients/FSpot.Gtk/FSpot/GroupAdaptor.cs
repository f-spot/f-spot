//
// GroupAdaptor.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
//
// Copyright (C) 2004-2007 Novell, Inc.
// Copyright (C) 2004 Larry Ewing
// Copyright (C) 2007 Thomas Van Machelen
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot
{
	public abstract class GroupAdaptor
	{
		public PhotoQuery Query { get; protected set; }

		public bool OrderAscending { get; set; }

		public abstract int Value (int item);
		public abstract int Count ();
		public abstract string TickLabel (int item);
		public abstract string GlassLabel (int item);

		protected abstract void Reload ();

		public abstract void SetGlass (int item);
		public abstract int IndexFromPhoto (IPhoto photo);
		public abstract IPhoto PhotoFromIndex (int item);

		public delegate void GlassSetHandler (GroupAdaptor adaptor, int index);
		public virtual event GlassSetHandler GlassSet;

		public delegate void ChangedHandler (GroupAdaptor adaptor);
		public virtual event ChangedHandler Changed;

		protected GroupAdaptor (PhotoQuery query, bool orderAscending)
		{
			OrderAscending = orderAscending;
			Query = query;
			Query.Changed += HandleQueryChanged;

			Reload ();
		}

		protected void HandleQueryChanged (IBrowsableCollection sender)
		{
			Reload ();
		}

		public void Dispose ()
		{
			Query.Changed -= HandleQueryChanged;
		}
	}
}
