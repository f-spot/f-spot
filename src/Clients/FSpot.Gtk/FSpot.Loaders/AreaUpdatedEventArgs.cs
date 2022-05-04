//
// AreaUpdatedEventArgs.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Loaders
{
	public class AreaUpdatedEventArgs : EventArgs
	{
		public Gdk.Rectangle Area { get; private set; }

		public AreaUpdatedEventArgs (Gdk.Rectangle area)
		{
			Area = area;
		}
	}
}
