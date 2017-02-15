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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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

		protected bool order_ascending = false;
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

		public abstract int Value (int item) ;
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
