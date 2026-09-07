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
using FSpot.Utils;

using Hyena;



namespace FSpot.Thumbnail
{
	class ImageThumbnailer : IThumbnailer
	{
		#region fields

		readonly SafeUri fileUri;
		readonly IImageFileFactory factory;
		readonly IFileSystem fileSystem;

		#endregion

		#region ctors

		public ImageThumbnailer (SafeUri fileUri, IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.fileUri = fileUri;
			this.factory = factory;
			this.fileSystem = fileSystem;
		}

		#endregion

		#region IThumbnailer implementation

		public bool TryCreateThumbnail (SafeUri thumbnailUri, ThumbnailSize size)
		{
			try {
				var imageFile = factory.Create (fileUri);
				return CreateThumbnail (thumbnailUri, size, imageFile);
			} catch {
				return false;
			}
		}

		#endregion

		#region private implementation

		bool CreateThumbnail (SafeUri thumbnailUri, ThumbnailSize size, IImageFile imageFile)
		{
			var pixels = size == ThumbnailSize.Normal ? 128 : 256;
			ImageBuffer buffer;
			try {
				buffer = imageFile.Load ();
			} catch (Exception e) {
				Logger.Log.Debug ($"Failed loading image for thumbnailing: {imageFile.Uri}");
				Logger.Log.Debug (e, "");
				return false;
			}

			double scale_x = (double)buffer.Width / pixels;
			double scale_y = (double)buffer.Height / pixels;
			double scale = Math.Max (1.0, Math.Max (scale_x, scale_y));
			int target_x = Math.Max ((int)(buffer.Width / scale), 1);
			int target_y = Math.Max ((int)(buffer.Height / scale), 1);

			var thumb = ImagingServices.Required.Scale (buffer, target_x, target_y);
			var mtime = fileSystem.File.GetMTime (imageFile.Uri).ToString ();
			ImagingServices.Required.SavePng (thumb, thumbnailUri.LocalPath,
				new string[] { ThumbnailPng.UriOptionKey, ThumbnailPng.MTimeOptionKey },
				new string[] { imageFile.Uri, mtime });

			return true;
		}

		#endregion
	}
}
