/*
 * Widgets.ViewContext.cs
 *
 * Author(s)
 *	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

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
