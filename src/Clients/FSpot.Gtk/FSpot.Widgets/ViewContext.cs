//
// ViewContext.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Widgets {
	// This nasty enum serves to differentiate between the different view
	// modes. As we have both SingleView and normal F-Spot, there is no
	// uniform way of naming these contexts.
	public enum ViewContext {
		Unknown,
		Single,
		Library,
		Edit,
		FullScreen
	}
}
