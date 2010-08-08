/*
 * FSpot.PhotoEventArgs.cs
 *
 * Author(s):
 *	Ruben Vermeersch
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using FSpot.Core;

namespace FSpot
{
	public class PhotoEventArgs : DbItemEventArgs<Photo> {
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
