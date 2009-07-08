//
// Fspot.Loaders.IImageLoader.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Stephane Delcroix  <sdelcroix@novell.com>
//	Ruben Vermeersch  <ruben@savanne.be>
//
// This is free software. See COPYING for details
//

using FSpot.Utils;
using System;
using Gdk;

namespace FSpot.Loaders {
	public class AreaPreparedEventArgs : EventArgs
	{
		bool reduced_resolution;

		public bool ReducedResolution {
			get { return reduced_resolution; }
		}
	
		public AreaPreparedEventArgs (bool reduced_resolution) : base ()
		{
			this.reduced_resolution = reduced_resolution;
		}
	}

	public class AreaUpdatedEventArgs : EventArgs
	{
		Gdk.Rectangle area;
		public Gdk.Rectangle Area { 
			get { return area; }
		}

		public AreaUpdatedEventArgs (Gdk.Rectangle area) : base ()
		{
			this.area = area;
		}
	}

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
