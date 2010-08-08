//
// Fspot.Loaders.IImageLoader.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Ruben Vermeersch  <ruben@savanne.be>
//
// This is free software. See COPYING for details
//

using FSpot.Utils;
using System;
using Gdk;
using Hyena;
using TagLib.Image;

namespace FSpot.Loaders {
	public interface IImageLoader : IDisposable {
		bool Loading { get; }

		event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		event EventHandler Completed;

		void Load (SafeUri uri);

		Pixbuf Pixbuf { get; }
		ImageOrientation PixbufOrientation { get; }
	}
}
