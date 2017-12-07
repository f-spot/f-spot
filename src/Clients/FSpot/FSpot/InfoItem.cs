//  InfoItem.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2017 Stehen Shaw.
//
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

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
