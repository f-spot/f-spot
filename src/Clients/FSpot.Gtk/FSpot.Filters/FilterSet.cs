//
// FilterSet.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/*
 * Filters/FilterSet.cs
 *
 * Authors:
 *   Larry Ewing <lewing@novell.com>
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * I don't like per file copyright notices.
 */

using System.Collections.Generic;

namespace FSpot.Filters
{
	public class FilterSet : IFilter
	{
		readonly List<IFilter> ifilters;

		public FilterSet ()
		{
			ifilters = new List<IFilter> ();
		}

		public void Add (IFilter filter)
		{
			ifilters.Add (filter);
		}

		public bool Convert (FilterRequest req)
		{
			bool changed = false;
			foreach (IFilter filter in ifilters) {
				changed |= filter.Convert (req);
			}
			return changed;
		}
	}
}
