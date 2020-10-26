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
	/// <summary>
	/// Used to aggregrate PhotoChanges and notifying the various ui pieces
	/// </summary>
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
			get => (changes & ~Changes.Data) != 0 || TagsChanged || VersionsChanged;
		}

		public bool DataChanged {
			get => (changes & Changes.Data) == Changes.Data;
			set {
				if (value)
					changes |= Changes.Data;
				else
					changes &= ~Changes.Data;
			}
		}
		public bool DefaultVersionIdChanged {
			get => (changes & Changes.DefaultVersionId) == Changes.DefaultVersionId;
			set {
				if (value) {
					changes |= Changes.DefaultVersionId;
					DataChanged = true;
				} else
					changes &= ~Changes.DefaultVersionId;
			}
		}
		public bool TimeChanged {
			get => (changes & Changes.Time) == Changes.Time;
			set {
				if (value)
					changes |= Changes.Time;
				else
					changes &= ~Changes.Time;
			}
		}
		public bool UriChanged {
			get => (changes & Changes.Uri) == Changes.Uri;
			set {
				if (value)
					changes |= Changes.Uri;
				else
					changes &= ~Changes.Uri;
			}
		}
		public bool RatingChanged {
			get => (changes & Changes.Rating) == Changes.Rating;
			set {
				if (value)
					changes |= Changes.Rating;
				else
					changes &= ~Changes.Rating;
			}
		}
		public bool DescriptionChanged {
			get => (changes & Changes.Description) == Changes.Description;
			set {
				if (value)
					changes |= Changes.Description;
				else
					changes &= ~Changes.Description;
			}
		}

		bool tagsChanged;
		public virtual bool TagsChanged {
			get => tagsChanged;
			private set { tagsChanged = value; }
		}

		bool versionsChanged;
		public virtual bool VersionsChanged {
			get => versionsChanged;
			private set { versionsChanged = value; }
		}

		public bool RollIdChanged {
			get => (changes & Changes.RollId) == Changes.RollId;
			set {
				if (value)
					changes |= Changes.RollId;
				else
					changes &= ~Changes.RollId;
			}
		}

		public bool MD5SumChanged {
			get => (changes & Changes.MD5Sum) == Changes.MD5Sum;
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
