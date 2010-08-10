//
// FSpot.Widgets.ApplicationActivatedEventArgs.cs
//
// Author(s):
//	Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (c) 2009 Novell, Inc.
//
// This is free software. See COPYING for details.
//

using System;
using GLib;

namespace FSpot.Widgets
{
	public class ApplicationActivatedEventArgs : EventArgs
	{
		AppInfo app;
		public AppInfo AppInfo {
			get { return app; }
		}

		public ApplicationActivatedEventArgs (AppInfo app) : base ()
		{
			this.app = app;
		}
	}
}

