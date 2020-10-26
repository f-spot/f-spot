//
// ILoadable.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

namespace FSpot.Core
{
	/// <summary>
	/// This is the contract that needs to be implemented before the image
	/// data of the object can be loaded.
	/// </summary>
	public interface ILoadable
	{
		SafeUri Uri { get; set; }
	}
}
