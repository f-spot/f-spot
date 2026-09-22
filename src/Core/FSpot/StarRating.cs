// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot
{
	public static class StarRating
	{
		public static IReadOnlyList<bool> Stars (uint rating)
		{
			var stars = new bool[5];
			int filled = rating > 5 ? 5 : (int)rating;
			for (int i = 0; i < filled; i++)
				stars[i] = true;
			return stars;
		}
	}
}
