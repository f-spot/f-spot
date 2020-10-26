//
// IPhoto.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Core
{
	public interface IPhoto
	{
		#region Metadata

		/// <summary>
		/// The time the item was created.
		/// </summary>
		System.DateTime Time { get; }

		/// <summary>
		/// The tags which are dedicated to this item.
		/// </summary>
		Tag[] Tags { get; }

		/// <summary>
		/// The description of the item.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// The name of the item.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The rating which is dedicted to this item. It should only range from 0 to 5.
		/// </summary>
		uint Rating { get; }

		#endregion


		#region Versioning

		/// <summary>
		/// The default version of this item. Every item must have at least one version and this must not be
		/// <see langref="null"/>
		/// </summary>
		IPhotoVersion DefaultVersion { get; }

		/// <summary>
		/// All versions of this item. Since every item must have at least the default version, this enumeration
		/// must not be empty.
		/// </summary>
		IEnumerable<IPhotoVersion> Versions { get; }

		#endregion
	}
}
