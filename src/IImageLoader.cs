//
// Fspot.IImageLoader.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Stephane Delcroix  <sdelcroix@novell.com>
//
// This is free software. See COPYING for details
//

using FSpot.Utils;
using System;
using Gdk;

namespace FSpot {
	public interface IImageLoader : IDisposable {
		bool Loading { get; }	

		event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		event EventHandler Completed;

		void Load (Uri uri);

		Pixbuf Pixbuf { get; }
		PixbufOrientation PixbufOrientation { get; }
	}
}
