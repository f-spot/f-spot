/*
 * ThumbnailFilenameCaptionRenderer.cs
 *
 * Author(s)
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;

using FSpot.Core;


namespace FSpot.Widgets
{

    public class ThumbnailFilenameCaptionRenderer : ThumbnailTextCaptionRenderer
    {
#region Constructor

        public ThumbnailFilenameCaptionRenderer ()
        {
        }

#endregion

#region Drawing Methods

        protected override string GetRenderText (IPhoto photo)
        {
            return Path.GetFileName (photo.DefaultVersion.Uri.LocalPath);
        }

#endregion

    }
}
