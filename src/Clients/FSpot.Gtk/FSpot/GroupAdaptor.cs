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
	public interface ILimitable
	{
		void SetLimits (int min, int max);
	}

	public abstract class GroupAdaptor
	{
		protected PhotoQuery query;
		public PhotoQuery Query { get { return query; } }

		protected bool order_ascending;
		public bool OrderAscending {
			get {
				return order_ascending;
			}
			set {
				if (order_ascending != value) {
					order_ascending = value;
				}
			}
		}

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

		protected void HandleQueryChanged (IBrowsableCollection sender)
		{
			Reload ();
		}

		public void Dispose ()
		{
			query.Changed -= HandleQueryChanged;
		}

		protected GroupAdaptor (PhotoQuery query, bool order_ascending)
		{
			this.order_ascending = order_ascending;
			this.query = query;
			this.query.Changed += HandleQueryChanged;

			Reload ();
		}
	}
}
