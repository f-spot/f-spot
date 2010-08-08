//
// Fspot.Loaders.AreaPreparedEventArgs.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Stephane Delcroix  <sdelcroix@novell.com>
//
// This is free software. See COPYING for details
//

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
}
