/*
 * IBrowsableItemVersion.cs
 * 
 * Author(s):
 *  Ruben Vermeersch <ruben@savanne.be>
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System.Collections.Generic;

namespace FSpot
{
	/// <summary>
	///    The interface adds functionality which is related to items where
	///    versions can be added or removed.
	/// </summary>
	public interface IBrowsableItemVersionable : IBrowsableItem{

		/// <summary>
		///    Sets the default version of a the item.
		/// </summary>
		/// <param name="version">
		///    A <see cref="IBrowsableItemVersion"/> which will be the new
		///    default version.
		/// </param>
		void SetDefaultVersion (IBrowsableItemVersion version);
	}
}
