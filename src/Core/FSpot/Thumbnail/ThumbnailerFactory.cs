//
// ThumbnailerFactory.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.FileSystem;
using FSpot.Imaging;

using Hyena;

namespace FSpot.Thumbnail
{
	class ThumbnailerFactory : IThumbnailerFactory
	{
		readonly IImageFileFactory factory;
		readonly IFileSystem fileSystem;

		public ThumbnailerFactory (IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.factory = factory;
			this.fileSystem = fileSystem;
		}

		public IThumbnailer GetThumbnailerForUri (SafeUri uri)
		{
			if (factory.HasLoader (uri))
				return new ImageThumbnailer (uri, factory, fileSystem);

			return null;
		}
	}
}
