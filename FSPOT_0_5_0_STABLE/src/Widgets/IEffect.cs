/*
 * IEffect.cs
 * 
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for details.
 */
using System;

namespace FSpot {
	public interface IEffect : IDisposable {
		bool OnExpose (Cairo.Context ctx, Gdk.Rectangle allocation);
	}
}
