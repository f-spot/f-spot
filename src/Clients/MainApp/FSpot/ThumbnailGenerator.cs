/*
 * FSpot.ThumbnailGenerator.cs
 *
 * Author(s)
 * 	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Hyena;
using FSpot.Utils;
using FSpot.Platform;

using Mono.Unix.Native;
using GFileInfo = GLib.FileInfo;

namespace FSpot {
    public class ThumbnailLoader : ImageLoaderThread {

        static public ThumbnailLoader Default = new ThumbnailLoader ();

        public void Request (SafeUri uri, ThumbnailSize size, int order)
        {
            var pixels = size == ThumbnailSize.Normal ? 128 : 256;
            Request (uri, order, pixels, pixels);
        }

        protected override void ProcessRequest (RequestItem request)
        {
            var size = request.Width == 128 ? ThumbnailSize.Normal : ThumbnailSize.Large;
            request.Result = XdgThumbnailSpec.LoadThumbnail (request.Uri, size);
        }
    }
}
