//
// AreaPreparedEventArgs.cs
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
	public class AreaPreparedEventArgs : EventArgs
	{
		public bool ReducedResolution { get; private set; }

		public AreaPreparedEventArgs (bool reducedResolution)
		{
			ReducedResolution = reducedResolution;
		}
	}
}