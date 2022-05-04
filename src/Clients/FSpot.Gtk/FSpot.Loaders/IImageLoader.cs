//
// IImageLoader.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gdk;

using Hyena;

using TagLib.Image;

namespace FSpot.Loaders
{
	public interface IImageLoader : IDisposable
	{
		bool Loading { get; }

		event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		event EventHandler Completed;

		void Load (SafeUri uri);

		Pixbuf Pixbuf { get; }
		ImageOrientation PixbufOrientation { get; }
	}
}
