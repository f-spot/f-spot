// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Interfaces;

namespace FSpot.Core
{
	public interface IPhoto : IMedia
	{
		/// <summary>
		///    The time the item was created.
		/// </summary>
		DateTime UtcTime { get; }

		/// <summary>
		///    The description of the item.
		/// </summary>
		string Description { get; }

		/// <summary>
		///    The name of the item.
		/// </summary>
		string Name { get; }

		/// <summary>
		///    The rating which is dedicated to this item. It should only range from 0 to 5.
		/// </summary>
		long Rating { get; }

		/// <summary>
		///    The default version of this item. Every item must have at least one version and this must not be
		///    <see langref="null"/>
		/// </summary>
		IPhotoVersion DefaultVersion { get; }

		/// <summary>
		///    All versions of this item. Since every item must have at least the default version, this enumeration
		///    must not be empty.
		/// </summary>
		List<IPhotoVersion> Versions { get; }
	}
}
