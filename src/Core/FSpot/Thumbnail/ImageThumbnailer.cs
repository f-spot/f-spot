//
// ImageThumbnailer.cs
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

using FSpot.FileSystem;
using FSpot.Imaging;

using Gdk;

using Hyena;

namespace FSpot.Thumbnail
{
	class ImageThumbnailer : IThumbnailer
	{
		readonly SafeUri fileUri;
		readonly IImageFileFactory factory;
		readonly IFileSystem fileSystem;

		public ImageThumbnailer (SafeUri fileUri, IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.fileUri = fileUri;
			this.factory = factory;
			this.fileSystem = fileSystem;
		}

		public bool TryCreateThumbnail (SafeUri thumbnailUri, ThumbnailSize size)
		{
			try {
				var imageFile = factory.Create (fileUri);
				return CreateThumbnail (thumbnailUri, size, imageFile);
			} catch {
				return false;
			}
		}

		bool CreateThumbnail (SafeUri thumbnailUri, ThumbnailSize size, IImageFile imageFile)
		{
			var pixels = size == ThumbnailSize.Normal ? 128 : 256;
			Pixbuf pixbuf;
			try {
				pixbuf = imageFile.Load ();
			} catch (Exception e) {
				Log.Debug ($"Failed loading image for thumbnailing: {imageFile.Uri}");
				Log.DebugException (e);
				return false;
			}

			double scale_x = (double)pixbuf.Width / pixels;
			double scale_y = (double)pixbuf.Height / pixels;
			double scale = Math.Max (1.0, Math.Max (scale_x, scale_y));
			// Ensures that the minimum value is 1 so that pixbuf.ScaleSimple doesn't return null
			// Seems to only happen in rare(?) cases
			int target_x = Math.Max ((int)(pixbuf.Width / scale), 1);
			int target_y = Math.Max ((int)(pixbuf.Height / scale), 1);

			var thumb_pixbuf = pixbuf.ScaleSimple (target_x, target_y, InterpType.Bilinear);
			var mtime = fileSystem.File.GetMTime (imageFile.Uri).ToString ();
			thumb_pixbuf.Savev (thumbnailUri.LocalPath, "png",
				new string[] { ThumbnailService.ThumbUriOpt, ThumbnailService.ThumbMTimeOpt, null },
				new string[] { imageFile.Uri, mtime });

			pixbuf.Dispose ();
			thumb_pixbuf.Dispose ();

			return true;
		}
	}
}
