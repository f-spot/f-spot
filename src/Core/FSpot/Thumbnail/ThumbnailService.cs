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

using Gdk;

using Hyena;

namespace FSpot.Thumbnail
{
	class ThumbnailService : IThumbnailService
	{
		readonly IXdgDirectoryService xdgDirectoryService;
		readonly IThumbnailerFactory thumbnailerFactory;
		readonly IFileSystem fileSystem;

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

		public Pixbuf GetThumbnail (SafeUri fileUri, ThumbnailSize size)
		{
			var thumbnailUri = GetThumbnailPath (fileUri, size);
			var thumbnail = LoadThumbnail (thumbnailUri);
			if (IsValid (fileUri, thumbnail))
				return thumbnail;
			IThumbnailer thumbnailer = thumbnailerFactory.GetThumbnailerForUri (fileUri);
			if (thumbnailer == null)
				return null;
			return !thumbnailer.TryCreateThumbnail (thumbnailUri, size)
				? null
				: LoadThumbnail (thumbnailUri);
		}

		public Pixbuf TryLoadThumbnail (SafeUri fileUri, ThumbnailSize size)
		{
			var thumbnailUri = GetThumbnailPath (fileUri, size);
			return LoadThumbnail (thumbnailUri);
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

		// internal for unit testing with Moq
		internal SafeUri GetThumbnailPath (SafeUri fileUri, ThumbnailSize size)
		{
			var fileHash = CryptoUtil.Md5Encode (fileUri.AbsoluteUri);
			return new SafeUri (Path.Combine (xdgDirectoryService.GetThumbnailsDir (size), fileHash + ".png"));
		}

		// internal for unit testing with Moq
		internal Pixbuf LoadThumbnail (SafeUri thumbnailUri)
		{
			if (!fileSystem.File.Exists (thumbnailUri))
				return null;
			try {
				return LoadPng (thumbnailUri);
			} catch (Exception e) {
				try {
					fileSystem.File.Delete (thumbnailUri);
				}
				// Analysis disable once EmptyGeneralCatchClause
				catch {
					// catch and ignore any errors on deleting thumbnails
					// e.g., unauthorized access, read-only filesystem
				}
				Log.Debug ($"Failed to load thumbnail: {thumbnailUri}");
				Log.DebugException (e);
				return null;
			}
		}

		internal const string ThumbMTimeOpt = "tEXt::Thumb::MTime";
		internal const string ThumbUriOpt = "tEXt::Thumb::URI";

		// internal for unit testing with Moq
		internal bool IsValid (SafeUri uri, Pixbuf pixbuf)
		{
			if (pixbuf == null)
				return false;

			if (pixbuf.GetOption (ThumbUriOpt) != uri.ToString ())
				return false;

			if (!fileSystem.File.Exists (uri))
				return false;

			var mTime = fileSystem.File.GetMTime (uri);
			return pixbuf.GetOption (ThumbMTimeOpt) == mTime.ToString ();
		}

		Pixbuf LoadPng (SafeUri uri)
		{
			using var stream = fileSystem.File.Read (uri);
			return new Pixbuf (stream);
		}
	}
}
