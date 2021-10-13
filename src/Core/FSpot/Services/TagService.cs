// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Interfaces;

using Tag = FSpot.Models.Tag;

namespace FSpot.Services
{
	public class TagService
	{
		static readonly Lazy<TagService> lazy = new Lazy<TagService> (() => new TagService ());
		public static TagService Instance { get => lazy.Value; }
		TagService () { }

		// This doesn't check if the tag is already there, use with caution.
		public void AddUnsafely (IMedia media, Tag tag)
		{
			if (media == null) throw new ArgumentNullException (nameof (media));
			if (tag == null) throw new ArgumentNullException (nameof (tag));

			media.Tags.Add (tag);
			//changes.AddTag (tag);
		}

		public void Add (IMedia media, Tag tag)
		{
			if (media == null) throw new ArgumentNullException (nameof (media));
			if (tag == null) throw new ArgumentNullException (nameof (tag));

			if (!media.Tags.Contains (tag))
				AddUnsafely (media, tag);
		}

		public void Add (IMedia media, IEnumerable<Tag> tags)
		{
			if (media == null) throw new ArgumentNullException(nameof (media));
			if (tags == null) throw new ArgumentNullException(nameof (tags));

			foreach (var tag in tags) {
				Add (media, tag);
			}
		}

		public void Remove (IMedia media, Tag tag)
		{
			if (media == null) throw new ArgumentNullException (nameof (media));
			if (tag == null) throw new ArgumentNullException (nameof (tag));

			if (!media.Tags.Contains (tag))
				return;

			media.Tags.Remove (tag);
			//changes.RemoveTag (tag);
		}

		public void Remove (IMedia media, IEnumerable<Tag> tags)
		{
			if (media == null) throw new ArgumentNullException (nameof (media));
			if (tags == null) throw new ArgumentNullException (nameof (tags));

			foreach (var tag in tags)
				Remove (media, tag);
		}

		public void RemoveCategory (IMedia media, IEnumerable<Tag> tags)
		{
			if (media == null) throw new ArgumentNullException (nameof (media));
			if (tags == null) throw new ArgumentNullException (nameof (tags));

			foreach (var tag in tags) {
				if (tag.IsCategory)
					RemoveCategory (media, tag.Children);

				Remove (media, tag);
			}
		}

		public bool Contains (IMedia media, Tag tag)
		{
			return media.Tags.Contains (tag);
		}
	}
}
