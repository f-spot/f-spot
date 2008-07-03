/*
 * FSpot.BrowsableEventArgs.cs
 * 
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot
{
	public class BrowsableEventArgs : System.EventArgs {
		private readonly int [] items;
		public int [] Items {
			get { return items; }
		}

		private readonly bool metadata_changed;
		public bool MetadataChanged {
			get { return metadata_changed; }
		}

		private readonly bool data_changed;
		public bool DataChanged {
			get { return data_changed; }
		}

		public BrowsableEventArgs (int num, bool metadata_changed, bool data_changed)
			: this (new int [] { num }, metadata_changed, data_changed)
		{
		}

		public BrowsableEventArgs (int [] items, bool metadata_changed, bool data_changed)
		{
			this.items = items;
			this.metadata_changed = metadata_changed;
			this.data_changed = data_changed;
		}

		[Obsolete ("You should be smarter and provide info about what changed!")]
		public BrowsableEventArgs (int num) : this (new int [] { num }, true, true)
		{
		}

		[Obsolete ("You should be smarter and provide info about what changed!")]
		public BrowsableEventArgs (int [] items) : this (items, true, true)
		{
		}
	}
}
