/*
 * FSpot.PhotoEventArgs.cs
 *
 * Author(s):
 *	Ruben Vermeersch
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot
{
	public class PhotoEventArgs : DbItemEventArgs {
		PhotosChanges changes;
		public PhotosChanges Changes {
			get { return changes; }
		}

		public PhotoEventArgs (Photo photo, PhotosChanges changes) : this (new Photo[] {photo}, changes)
		{
		}

		public PhotoEventArgs (Photo[] photos, PhotosChanges changes) : base (photos)
		{
			this.changes = changes;
		}
	}
}
