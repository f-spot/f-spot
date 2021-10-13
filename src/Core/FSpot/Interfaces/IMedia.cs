// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FSpot.Models;

namespace FSpot.Interfaces
{
	public interface IMedia
	{
		/// <summary>
		/// Tags associated with this media item
		/// </summary>
		List<Tag> Tags { get; }
	}
}
