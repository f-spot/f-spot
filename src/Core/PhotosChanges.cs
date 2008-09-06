/*
 * FSpot.PhotosChanges.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

namespace FSpot
{
	//used to aggregate PhotoChanges and notifying the various ui pieces
	public class PhotosChanges : IBrowsableItemChanges
	{
		[Flags ()]
		enum Changes {
			None 			= 0x0,
			DefaultVersionId 	= 0x1,
			Time			= 0x2,
			Uri			= 0x4,
			Rating			= 0x8,
			Description		= 0x10,
			RollId			= 0x20,
			Data			= 0x40,
			MD5Sum			= 0x80
		}

		Changes changes = Changes.None;

		public bool MetadataChanged {
			get { return (changes & ~Changes.Data) != Changes.None || TagsChanged || VersionsChanged; }
		}

		public bool DataChanged {
			get { return (changes & Changes.Data) == Changes.Data; }
			set {
				if (value)
					changes |= Changes.Data;
				else
					changes &= ~Changes.Data;
			}
		}
		public bool DefaultVersionIdChanged {
			get { return (changes & Changes.DefaultVersionId) == Changes.DefaultVersionId; }
			set {
				if (value) {
					changes |= Changes.DefaultVersionId;
					DataChanged = true;
				} else
					changes &= ~Changes.DefaultVersionId;
			}
		}
		public bool TimeChanged {
			get { return (changes & Changes.Time) == Changes.Time; }
			set {
				if (value)
					changes |= Changes.Time;
				else
					changes &= ~Changes.Time;
			}
		}
		public bool UriChanged {
			get { return (changes & Changes.Uri) == Changes.Uri; }
			set {
				if (value)
					changes |= Changes.Uri;
				else
					changes &= ~Changes.Uri;
			}
		}
		public bool RatingChanged {
			get { return (changes & Changes.Rating) == Changes.Rating; }
			set {
				if (value)
					changes |= Changes.Rating;
				else
					changes &= ~Changes.Rating;
			}
		}
		public bool DescriptionChanged {
			get { return (changes & Changes.Description) == Changes.Description; }
			set {
				if (value)
					changes |= Changes.Description;
				else
					changes &= ~ Changes.Description;
			}
		}
		bool tags_changed = false;
		public bool TagsChanged {
			get { return tags_changed; }
			private set { tags_changed = value; }
		}
		bool versions_changed = false;
		public virtual bool VersionsChanged {
			get { return versions_changed; }
			private set { versions_changed = value; }
		}
		public bool RollIdChanged {
			get { return (changes & Changes.RollId) == Changes.RollId; }
			set {
				if (value)
					changes |= Changes.RollId;
				else
					changes &= ~Changes.RollId;
			}
		}

		public bool MD5SumChanged {
			get { return (changes & Changes.MD5Sum) == Changes.MD5Sum ; } 
			set {
				if (value)
				 	changes |= Changes.MD5Sum;
				else
				 	changes &= ~Changes.MD5Sum; 
			}
		 
		}

		public static PhotosChanges operator | (PhotosChanges c1, PhotosChanges c2)
		{
			PhotosChanges changes = new PhotosChanges ();
			changes.changes = c1.changes | c2.changes;
			changes.VersionsChanged = c1.VersionsChanged || c2.VersionsChanged;
			changes.TagsChanged = c1.TagsChanged || c2.TagsChanged;
			return changes;
		}

		public PhotosChanges ()
		{
		}
	}
}
