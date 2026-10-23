// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Core;

namespace FSpot.Query
{
	public sealed class TimelineMonth
	{
		public TimelineMonth (DateTime start, DateTime end, int count)
		{
			Start = start;
			End = end;
			Count = count;
		}

		public DateTime Start { get; }
		public DateTime End { get; }
		public int Count { get; }
	}

	public static class TimelineMonths
	{
		public static IReadOnlyList<TimelineMonth> Group (IEnumerable<IPhoto> photos)
		{
			return photos
				.GroupBy (photo => new DateTime (photo.Time.Year, photo.Time.Month, 1))
				.OrderBy (group => group.Key)
				.Select (group => new TimelineMonth (group.Key, group.Key.AddMonths (1), group.Count ()))
				.ToList ();
		}

		public static IReadOnlyList<TimelineMonth> Span (IEnumerable<IPhoto> photos)
			=> Span (photos.Select (photo => photo.Time));

		public static IReadOnlyList<TimelineMonth> Span (IEnumerable<DateTime> times)
		{
			var grouped = times
				.GroupBy (time => new DateTime (time.Year, time.Month, 1))
				.OrderBy (group => group.Key)
				.Select (group => new TimelineMonth (group.Key, group.Key.AddMonths (1), group.Count ()))
				.ToList ();
			return grouped;
		}

		public static TimelineMonth InclusiveRange (IReadOnlyList<TimelineMonth> months, int minIndex, int maxIndex)
		{
			if (months == null || months.Count == 0)
				return new TimelineMonth (DateTime.MinValue, DateTime.MinValue, 0);

			int lo = Math.Min (minIndex, maxIndex);
			int hi = Math.Max (minIndex, maxIndex);
			if (lo < 0)
				lo = 0;
			if (hi >= months.Count)
				hi = months.Count - 1;
			return new TimelineMonth (months[lo].Start, months[hi].End, 0);
		}
	}
}
