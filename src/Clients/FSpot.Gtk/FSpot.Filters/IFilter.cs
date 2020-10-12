//
// IFilter.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Filters
{
	public interface IFilter
	{
		bool Convert (FilterRequest request);
	}
}
