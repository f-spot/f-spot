//
// IImageFile.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using TagLib.Image;

namespace FSpot.Imaging
{
	public interface IImageFile : IDisposable
	{
		SafeUri Uri { get; }
		ImageOrientation Orientation { get; }

		Gdk.Pixbuf Load ();
		Cms.Profile GetProfile ();
		Gdk.Pixbuf Load (int maxWidth, int maxHeight);
		Stream PixbufStream ();
	}
}
