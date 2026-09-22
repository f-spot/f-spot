// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Thumbnail;

namespace FSpot
{
	public static class ThumbnailRefresh
	{
		public static void Invalidate (IThumbnailService thumbnails, IPhoto photo)
		{
			if (thumbnails == null)
				throw new ArgumentNullException (nameof (thumbnails));
			if (photo?.DefaultVersion?.Uri == null)
				return;
			thumbnails.DeleteThumbnails (photo.DefaultVersion.Uri);
		}
	}
}
