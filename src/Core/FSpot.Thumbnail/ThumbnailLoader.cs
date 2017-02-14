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
using FSpot.Imaging;
using FSpot.Thumbnail;
using Hyena;

namespace FSpot.Thumbnail
{
	class ThumbnailLoader : ImageLoaderThread, IThumbnailLoader
	{
		readonly IThumbnailService thumbnailService;

		public ThumbnailLoader (IImageFileFactory imageFileFactory, IThumbnailService thumbnailService)
			: base (imageFileFactory)
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
