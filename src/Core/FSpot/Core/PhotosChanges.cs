//
// PhotosChanges.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Core
{
	//used to aggregate PhotoChanges and notifying the various ui pieces
	public class PhotosChanges : IBrowsableItemChanges
	{
		[Flags ()]
		enum Changes
		{
			DefaultVersionId = 0x1,
			Time = 0x2,
			Uri = 0x4,
			Rating = 0x8,
			Description = 0x10,
			RollId = 0x20,
			Data = 0x40,
			MD5Sum = 0x80
		}

		Changes changes;

		public bool MetadataChanged {
			get { return (changes & ~Changes.Data) != 0 || TagsChanged || VersionsChanged; }
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
					changes &= ~Changes.Description;
			}
		}

		bool tags_changed;
		public virtual bool TagsChanged {
			get { return tags_changed; }
			private set { tags_changed = value; }
		}

		bool versions_changed;
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
			get { return (changes & Changes.MD5Sum) == Changes.MD5Sum; }
			set {
				if (value)
					changes |= Changes.MD5Sum;
				else
					changes &= ~Changes.MD5Sum;
			}

		}

		public static PhotosChanges operator | (PhotosChanges c1, PhotosChanges c2)
		{
			if (c1 == null)
				throw new ArgumentNullException (nameof (c1));
			if (c2 == null)
				throw new ArgumentNullException (nameof (c2));

			var changes = new PhotosChanges {
				changes = c1.changes | c2.changes,
				VersionsChanged = c1.VersionsChanged || c2.VersionsChanged,
				TagsChanged = c1.TagsChanged || c2.TagsChanged
			};
			return changes;
		}
	}
}
