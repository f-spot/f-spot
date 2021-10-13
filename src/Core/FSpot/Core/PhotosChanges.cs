//
// PhotosChanges.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using System;

namespace FSpot.Core
{
	//used to aggregate PhotoChanges and notifying the various ui pieces
	public class PhotosChanges : IBrowsableItemChanges
	{
		[Flags]
		enum Changes {
			DefaultVersionId = 0x1,
			Time             = 0x2,
			Uri              = 0x4,
			Rating           = 0x8,
			Description      = 0x10,
			RollId           = 0x20,
			Data             = 0x40,
			MD5Sum           = 0x80
		}

		Changes changes = 0;

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
					changes &= ~ Changes.Description;
			}
		}

		public virtual bool TagsChanged { get; private set; }

		public virtual bool VersionsChanged { get; private set; }

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

		public static PhotosChanges operator | (PhotosChanges photoChanges1, PhotosChanges photoChanges2)
		{
			if (photoChanges1 == null)
				throw new ArgumentNullException (nameof(photoChanges1));
			if (photoChanges2 == null)
				throw new ArgumentNullException (nameof(photoChanges2));

			var changes = new PhotosChanges {
				changes = photoChanges1.changes | photoChanges2.changes,
				VersionsChanged = photoChanges1.VersionsChanged || photoChanges2.VersionsChanged,
				TagsChanged = photoChanges1.TagsChanged || photoChanges2.TagsChanged
			};

			return changes;
		}
	}
}
