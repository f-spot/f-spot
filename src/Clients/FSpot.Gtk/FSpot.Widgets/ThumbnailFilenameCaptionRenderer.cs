//
// ThumbnailFilenameCaptionRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Core;

namespace FSpot.Widgets
{
	public class ThumbnailFilenameCaptionRenderer : ThumbnailTextCaptionRenderer
	{
		#region Drawing Methods

		protected override string GetRenderText (IPhoto photo)
		{
			return Path.GetFileName (photo.DefaultVersion.Uri.LocalPath);
		}

		#endregion
	}
}
