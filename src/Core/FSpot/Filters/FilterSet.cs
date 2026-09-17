// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Filters
{
	public class FilterSet : IFilter
	{
		readonly List<IFilter> ifilters = new List<IFilter> ();

		public void Add (IFilter filter)
		{
			ifilters.Add (filter);
		}

		public bool Convert (FilterRequest req)
		{
			bool changed = false;
			foreach (IFilter filter in ifilters)
				changed |= filter.Convert (req);
			return changed;
		}
	}
}
