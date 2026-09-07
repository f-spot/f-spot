//
// IThumbnailFactory.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using Hyena;

namespace FSpot.Thumbnail
{
	public interface IThumbnailService
	{
		ImageBuffer GetThumbnail (SafeUri fileUri, ThumbnailSize size);
		ImageBuffer TryLoadThumbnail (SafeUri fileUri, ThumbnailSize size);
		void DeleteThumbnails (SafeUri fileUri);
	}
}
