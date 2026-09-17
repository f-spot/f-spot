// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Filters
{
	public interface IFilter
	{
		bool Convert (FilterRequest request);
	}
}
