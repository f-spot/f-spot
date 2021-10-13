// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;
using FSpot.Models;
using FSpot.Services;
using FSpot.Utils;

using Mono.Unix;

namespace FSpot.Import
{
	internal class MetadataImporter
	{
		readonly TagStore tagStore;
		readonly Stack<Tag> tagsCreated;

		const string LastImportIcon = "gtk-new";

		class TagInfo
		{
			// This class contains the Root tag name, and its Icon name (if any)
			public string TagName { get; }
			public string IconName { get; }

			public bool HasIcon {
				get { return IconName != null; }
			}

			public TagInfo (string tagName, string iconName)
			{
				TagName = tagName;
				IconName = iconName;
			}

			public TagInfo (string tagName)
			{
				TagName = tagName;
				IconName = null;
			}
		}

		readonly TagInfo lastImportRootTag;

		public MetadataImporter (TagStore tagStore)
		{
			this.tagStore = tagStore;
			tagsCreated = new Stack<Tag> ();

			lastImportRootTag = new TagInfo (Catalog.GetString ("Imported Tags"), LastImportIcon);
		}

		Tag EnsureTag (TagInfo info, Tag parent)
		{
			Tag tag = tagStore.GetTagByName (info.TagName);

			if (tag != null)
				return tag;

			tag = tagStore.CreateTag (parent, info.TagName, false, true);

			if (info.HasIcon) {
				tag.ThemeIconName = info.IconName;
				tagStore.Commit (tag);
			}

			tagsCreated.Push (tag);
			return tag;
		}

		void AddTagToPhoto (Photo photo, string newTagName)
		{
			if (string.IsNullOrEmpty (newTagName))
				return;

			Tag parent = EnsureTag (lastImportRootTag, tagStore.RootCategory);
			Tag tag = EnsureTag (new TagInfo (newTagName), parent);

			// Now we have the tag for this place, add the photo to it
			TagService.Instance.Add (photo, tag);
		}

		public bool Import (Photo photo, IPhoto importingFrom)
		{
			using var metadata = MetadataUtils.Parse (importingFrom.DefaultVersion.Uri);
			if (metadata == null)
				return true;

			// Copy Rating
			var rating = metadata.ImageTag.Rating;
			if (rating.HasValue) {
				var ratingVal = Math.Min (metadata.ImageTag.Rating.Value, 5);
				photo.Rating = Math.Max (0, ratingVal);
			}

			// Copy Keywords
			foreach (var keyword in metadata.ImageTag.Keywords) {
				AddTagToPhoto (photo, keyword);
			}

			// XXX: We might want to copy more data.
			return true;
		}

		public void Cancel ()
		{
			// User have cancelled the import.
			// Remove all created tags
			while (tagsCreated.Count > 0)
				tagStore.Remove (tagsCreated.Pop ());

			// Clear the tagsCreated array
			tagsCreated.Clear ();
		}

		public void Finish ()
		{
			// Clear the tagsCreated array, since we do not need it anymore.
			tagsCreated.Clear ();
		}
	}
}
