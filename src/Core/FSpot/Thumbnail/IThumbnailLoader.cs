//
// IThumbnailLoader.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

namespace FSpot.Thumbnail
{
	public interface IThumbnailLoader : IImageLoaderThread
	{
		void Request (SafeUri uri, ThumbnailSize size, int order);
	}
}
