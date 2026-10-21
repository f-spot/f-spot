// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Import;
using FSpot.Thumbnail;

namespace FSpot
{
	// The process creates this once. Nothing is discovered at runtime.
	public static class CoreServices
	{
		public static ServiceGraph CreateDefault ()
		{
			var files = new DotNetFileSystem ();
			var images = new ImageFileFactory (files);
			var directories = new XdgDirectoryService (files, new EnvironmentAdapter ());
			var thumbnailers = new ThumbnailerFactory (images, files);
			var thumbnails = new ThumbnailService (directories, thumbnailers, files);
			var loader = new ThumbnailLoader (images, thumbnails);
			return new ServiceGraph (files, images, thumbnails, () => new ImportController (files, loader));
		}
	}

	public sealed class ServiceGraph
	{
		public ServiceGraph (IFileSystem fileSystem, IImageFileFactory images, IThumbnailService thumbnails, Func<IImportController> import)
		{
			FileSystem = fileSystem;
			Images = images;
			Thumbnails = thumbnails;
			Import = import;
		}

		public IFileSystem FileSystem { get; }
		public IImageFileFactory Images { get; }
		public IThumbnailService Thumbnails { get; }
		public Func<IImportController> Import { get; }
	}
}
