//
// Fspot.Loaders.AreaUpdatedEventArgs.cs
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
}
