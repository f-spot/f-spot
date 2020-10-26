//
// IPhotoVersionable.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Core
{
	/// <summary>
	/// The interface adds functionality which is related to items where
	/// versions can be added or removed.
	/// </summary>
	public interface IPhotoVersionable : IPhoto
	{
		/// <summary>
		/// Sets the default version of a the item.
		/// </summary>
		/// <param name="version">
		/// A <see cref="IBrowsableItemVersion"/> which will be the new
		/// default version.
		/// </param>
		void SetDefaultVersion (IPhotoVersion version);
	}
}
