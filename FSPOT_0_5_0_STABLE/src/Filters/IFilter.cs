/*
 * Filters/IFilter.cs
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 *
 */

namespace FSpot.Filters {
	public interface IFilter
	{
		bool Convert (FilterRequest request);
	}
}
