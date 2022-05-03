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
using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot.Import
{
	class MetadataImporter
	{
		TagStore tagStore;
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

			lastImportRootTag = new TagInfo (Strings.ImportedTags, LastImportIcon);
		}

		Tag EnsureTag (TagInfo info, Category parent)
		{
			Tag tag = tagStore.GetTagByName (info.TagName);

			if (tag != null)
				return tag;

			tag = tagStore.CreateCategory (parent, info.TagName, false);

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
			Tag tag = EnsureTag (new TagInfo (newTagName), parent as Category);

			// Now we have the tag for this place, add the photo to it
			photo.AddTag (tag);
		}

		public bool Import (Photo photo, IPhoto importingFrom)
		{
			using (var metadata = MetadataUtils.Parse (importingFrom.DefaultVersion.Uri)) {
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
