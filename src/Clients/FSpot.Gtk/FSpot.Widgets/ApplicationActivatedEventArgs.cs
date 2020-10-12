//
// ApplicationActivatedEventArgs.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using GLib;

namespace FSpot.Widgets
{
	public class ApplicationActivatedEventArgs : EventArgs
	{
		public AppInfo AppInfo { get; private set; }

		public ApplicationActivatedEventArgs (AppInfo app)
		{
			AppInfo = app;
		}
	}
}
