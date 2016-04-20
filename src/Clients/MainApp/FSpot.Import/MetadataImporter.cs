//
// MetadataImporter.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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
using System.Collections.Generic;
using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;
using Mono.Unix;

namespace FSpot.Import
{
	class MetadataImporter
	{
		TagStore tag_store;
		readonly Stack<Tag> tags_created;

		const string LastImportIcon = "gtk-new";

		class TagInfo {
			// This class contains the Root tag name, and its Icon name (if any)
			public string TagName { get; private set; }
			public string IconName { get; private set; }

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
		} // TagInfo

		TagInfo li_root_tag; // This is the Last Import root tag

		public MetadataImporter (TagStore tagStore)
		{
			tag_store = tagStore;
			tags_created = new Stack<Tag> ();

			li_root_tag = new TagInfo (Catalog.GetString ("Imported Tags"), LastImportIcon);
		}

		Tag EnsureTag (TagInfo info, Category parent)
		{
			Tag tag = tag_store.GetTagByName (info.TagName);

			if (tag != null)
				return tag;

			tag = tag_store.CreateCategory (parent,
					info.TagName,
					false);

			if (info.HasIcon) {
				tag.ThemeIconName = info.IconName;
				tag_store.Commit(tag);
			}

			tags_created.Push (tag);
			return tag;
		}

		void AddTagToPhoto (Photo photo, string newTagName)
		{
			if (string.IsNullOrEmpty(newTagName))
				return;

			Tag parent = EnsureTag (li_root_tag, tag_store.RootCategory);
			Tag tag = EnsureTag (new TagInfo (newTagName), parent as Category);

			// Now we have the tag for this place, add the photo to it
			photo.AddTag (tag);
		}

		public bool Import (Photo photo, IPhoto importingFrom)
		{
			using (var metadata = Metadata.Parse (importingFrom.DefaultVersion.Uri)) {
				if (metadata == null)
					return true;

				// Copy Rating
				var rating = metadata.ImageTag.Rating;
				if (rating.HasValue) {
					var rating_val = Math.Min (metadata.ImageTag.Rating.Value, 5);
					photo.Rating = Math.Max (0, rating_val);
				}

				// Copy Keywords
				foreach (var keyword in metadata.ImageTag.Keywords) {
					AddTagToPhoto (photo, keyword);
				}

				// XXX: We might want to copy more data.
			}
			return true;
		}

		public void Cancel()
		{
			// User have cancelled the import.
			// Remove all created tags
			while (tags_created.Count > 0)
				tag_store.Remove (tags_created.Pop());

			// Clear the tags_created array
			tags_created.Clear();
		}

		public void Finish()
		{
			// Clear the tags_created array, since we do not need it anymore.
			tags_created.Clear();
		}
	}
} // namespace
