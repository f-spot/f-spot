// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

namespace FSpot.Query
{
	public static class QueryChipFormat
	{
		public static string Rating (uint? min, uint? max)
			=> string.Format (Strings.RatingRangeChip, min ?? 0, max ?? 5);

		public static string Date (DateTime? start, DateTime? end)
			=> string.Format (Strings.DateRangeChip, start?.ToString ("yyyy-MM-dd"), end?.ToString ("yyyy-MM-dd"));

		public static string Roll (string ids)
			=> string.Format (Strings.ImportRollChip, Strings.ImportRoll, ids);

		public static string TimelineMonth (DateTime start, int count)
			=> count > 0
				? string.Format (Strings.TimelineMonthCount, start.ToString ("MMMM yyyy"), count)
				: start.ToString ("MMMM yyyy");
	}
}
