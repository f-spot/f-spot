//  InfoItem.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2017 Stehen Shaw.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Widgets;

namespace FSpot
{
	public class InfoItem : InfoBox
    {
        readonly BrowsablePointer item;

        public InfoItem (BrowsablePointer item)
		{
			this.item = item;
			item.Changed += HandleItemChanged;
			HandleItemChanged (item, null);
			VersionChanged += HandleVersionChanged;
			ShowTags = true;
			ShowRating = true;
			Context = ViewContext.FullScreen;
		}

		void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Photo = item.Current;
		}

		void HandleVersionChanged (InfoBox box, IPhotoVersion version)
		{
			var versionable = item.Current as IPhotoVersionable;
			var q = item.Collection as PhotoQuery;

			if (versionable != null && q != null) {
				versionable.SetDefaultVersion (version);
				q.Commit (item.Index);
			}
		}
	}
}
