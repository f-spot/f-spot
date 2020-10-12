//
// ImageLoader.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using FSpot.Imaging;
using FSpot.Utils;
using Hyena;

namespace FSpot.Loaders
{
	public static class ImageLoader
	{
		public static IImageLoader Create (SafeUri uri)
		{
			if (!App.Instance.Container.Resolve<IImageFileFactory> ().HasLoader (uri))
				throw new Exception ("Loader requested for unknown file type: " + uri.GetExtension());

			return new GdkImageLoader ();
		}
	}
}
