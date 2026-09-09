//
// ThumbnailService.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Utils;

using Hyena;



namespace FSpot.Thumbnail
{
	class ThumbnailService : IThumbnailService
	{
		#region fields

		readonly IXdgDirectoryService xdgDirectoryService;
		readonly IThumbnailerFactory thumbnailerFactory;
		readonly IFileSystem fileSystem;

		#endregion

		#region ctors

		public ThumbnailService (IXdgDirectoryService xdgDirectoryService, IThumbnailerFactory thumbnailerFactory, IFileSystem fileSystem)
		{
			this.xdgDirectoryService = xdgDirectoryService;
			this.thumbnailerFactory = thumbnailerFactory;
			this.fileSystem = fileSystem;

			var large = new SafeUri (Path.Combine (xdgDirectoryService.GetThumbnailsDir (ThumbnailSize.Large)));
			if (!fileSystem.Directory.Exists (large))
				fileSystem.Directory.CreateDirectory (large);

			var normal = new SafeUri (Path.Combine (xdgDirectoryService.GetThumbnailsDir (ThumbnailSize.Normal)));
			if (!fileSystem.Directory.Exists (normal))
				fileSystem.Directory.CreateDirectory (normal);
		}

		#endregion

		#region public API

		public ImageBuffer GetThumbnail (SafeUri fileUri, ThumbnailSize size)
		{
			var thumbnailUri = GetThumbnailPath (fileUri, size);
			var thumbnail = LoadThumbnail (thumbnailUri);
			if (IsValid (fileUri, thumbnail))
				return thumbnail.Image;
			IThumbnailer thumbnailer = thumbnailerFactory.GetThumbnailerForUri (fileUri);
			if (thumbnailer == null)
				return null;
			if (!thumbnailer.TryCreateThumbnail (thumbnailUri, size))
				return null;
			var created = LoadThumbnail (thumbnailUri);
			return created?.Image;
		}

		public ImageBuffer TryLoadThumbnail (SafeUri fileUri, ThumbnailSize size)
		{
			var thumbnailUri = GetThumbnailPath (fileUri, size);
			return LoadThumbnail (thumbnailUri)?.Image;
		}

		public void DeleteThumbnails (SafeUri fileUri)
		{
			Enum.GetValues (typeof (ThumbnailSize))
				.OfType<ThumbnailSize> ()
				.Select (size => GetThumbnailPath (fileUri, size))
				.ToList ()
				.ForEach (thumbnailUri => {
					if (fileSystem.File.Exists (thumbnailUri)) {
						try {
							fileSystem.File.Delete (thumbnailUri);
						}
						// Analysis disable once EmptyGeneralCatchClause
						catch {
							// catch and ignore any errors on deleting thumbnails
							// e.g., unauthorized access, read-only filesystem
						}
					}
				});
		}

		#endregion

		#region private implementation

		// internal for unit testing with Moq
		internal SafeUri GetThumbnailPath (SafeUri fileUri, ThumbnailSize size)
		{
			var fileHash = HashUtils.Md5Encode (fileUri.AbsoluteUri);
			return new SafeUri (Path.Combine (xdgDirectoryService.GetThumbnailsDir (size), fileHash + ".png"));
		}

		// internal for unit testing with Moq
		internal ThumbnailPng LoadThumbnail (SafeUri thumbnailUri)
		{
			if (!fileSystem.File.Exists (thumbnailUri))
				return null;
			try {
				using (var stream = fileSystem.File.Read (thumbnailUri))
					return ImagingServices.Required.LoadPng (stream);
			} catch (Exception e) {
				try {
					fileSystem.File.Delete (thumbnailUri);
				}
				// Analysis disable once EmptyGeneralCatchClause
				catch {
					// catch and ignore any errors on deleting thumbnails
					// e.g., unauthorized access, read-only filesystem
				}
				Logger.Log.Debug ($"Failed to load thumbnail: {thumbnailUri}");
				Logger.Log.Debug (e, "");
				return null;
			}
		}

		// internal for unit testing with Moq
		internal bool IsValid (SafeUri uri, ThumbnailPng png)
		{
			if (png == null)
				return false;

			if (png.UriOption != uri.ToString ())
				return false;

			if (!fileSystem.File.Exists (uri))
				return false;

			var mTime = fileSystem.File.GetMTime (uri);
			return png.MTimeOption == mTime.ToString ();
		}

		#endregion
	}
}
