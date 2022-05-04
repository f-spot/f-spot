//
// PixbufMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Thumbnail;

using Gdk;

using Hyena;

namespace Mocks
{
	public static class PixbufMock
	{
		public static byte[] CreateThumbnail (SafeUri uri, DateTime mTime)
		{
			var pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, 1, 1);
			return pixbuf.SaveToBuffer ("png", new[] {
				ThumbnailService.ThumbUriOpt,
				ThumbnailService.ThumbMTimeOpt,
				null
			}, new[] {
				uri,
				mTime.ToString ()
			});
		}

		public static Pixbuf CreatePixbuf (SafeUri uri, DateTime mTime)
		{
			using var stream = new MemoryStream (CreateThumbnail (uri, mTime));

			return new Pixbuf (stream);
		}
	}
}
