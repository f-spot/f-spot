/*
 * IBrowsableItem.cs
 * 
 * Author(s):
 *  Larry Ewing <lewing@novell.com>
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System.Collections.Generic;

using Hyena;


namespace FSpot
{

	public interface IBrowsableItem
	{

#region Metadata

		/// <summary>
		///    The time the item was created.
		/// </summary>
		System.DateTime Time { get; }

		/// <summary>
		///    The tags which are dedicated to this item.
		/// </summary>
		Tag [] Tags { get; }

		/// <summary>
		///    The description of the item.
		/// </summary>
		string Description { get; }

		/// <summary>
		///    The name of the item.
		/// </summary>
		string Name { get; }

		/// <summary>
		///    The rating which is dedicted to this item. It should only range from 0 to 5.
		/// </summary>
		uint Rating { get; }

#endregion


#region Versioning

		/// <summary>
		///    The default version of this item. Every item must have at least one version and this must not be
		///    <see langref="null"/>
		/// </summary>
		IBrowsableItemVersion DefaultVersion { get; }

		/// <summary>
		///    All versions of this item. Since every item must have at least the default version, this enumeration
		///    must not be empty.
		/// </summary>
		IEnumerable<IBrowsableItemVersion> Versions { get; }

#endregion

	}
}
