//
// ThumbnailLoader.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2008-2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

namespace FSpot.Thumbnail
{
	class ThumbnailLoader : ImageLoaderThread, IThumbnailLoader
	{
		readonly IThumbnailService thumbnailService;

		public ThumbnailLoader (IImageFileFactory imageFileFactory, IThumbnailService thumbnailService) : base (imageFileFactory)
		{
			this.thumbnailService = thumbnailService;
		}

		public void Request (SafeUri uri, ThumbnailSize size, int order)
		{
			var pixels = size == ThumbnailSize.Normal ? 128 : 256;
			Request (uri, order, pixels, pixels);
		}

		protected override void ProcessRequest (RequestItem request)
		{
			var size = request.Width == 128 ? ThumbnailSize.Normal : ThumbnailSize.Large;
			request.Result = thumbnailService.GetThumbnail (request.Uri, size);
		}
	}
}
