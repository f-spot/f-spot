/*
 * ITranstion.cs
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * Larry had a little lamb
 * 
 */
using System;

namespace FSpot {
	public interface ITransition : IDisposable {
		bool OnExpose (Cairo.Context ctx, Gdk.Rectangle allocation);
		bool OnEvent (Gtk.Widget w);
		int Frames { get; }
	}
}
