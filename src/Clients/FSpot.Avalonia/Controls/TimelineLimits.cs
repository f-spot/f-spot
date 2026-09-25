//
// TimelineLimits.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Controls;

public enum TimelineLimit
{
	None,
	Min,
	Max
}

/// <summary>
/// GTK <c>GroupSelector.Limit</c>: two handles on the month strip that bound the date range.
/// The min handle sits on the left edge of its month and the max handle on the right edge.
/// </summary>
public static class TimelineLimits
{
	/// <summary>GTK <c>Limit.width</c>.</summary>
	public const double HandleWidth = 10;

	/// <summary>GTK <c>Limit.handle_height</c>.</summary>
	public const double HandleHeight = 10;

	public static double EdgeX (TimelineLimit limit, int position, double columnWidth)
		=> (limit == TimelineLimit.Max ? position + 1 : position) * columnWidth;

	/// <summary>
	/// Which handle is under <paramref name="x"/>. When both handles share an edge the max
	/// handle wins on the right half so a one-month range can still be widened either way.
	/// </summary>
	public static TimelineLimit HitTest (double x, int min, int max, double columnWidth)
	{
		if (columnWidth <= 0)
			return TimelineLimit.None;
		double half = HandleWidth / 2;
		double minX = EdgeX (TimelineLimit.Min, min, columnWidth);
		double maxX = EdgeX (TimelineLimit.Max, max, columnWidth);
		bool onMin = Math.Abs (x - minX) <= half;
		bool onMax = Math.Abs (x - maxX) <= half;
		if (onMin && onMax)
			return x >= (minX + maxX) / 2 ? TimelineLimit.Max : TimelineLimit.Min;
		if (onMin)
			return TimelineLimit.Min;
		return onMax ? TimelineLimit.Max : TimelineLimit.None;
	}

	/// <summary>
	/// GTK <c>Manipulator.EndDrag</c>: the handle lands on the month under the middle of the
	/// dragged handle. The result keeps min at or before max.
	/// </summary>
	public static (int Min, int Max) Drop (TimelineLimit limit, double x, int min, int max, double columnWidth, int count)
	{
		if (count <= 0 || columnWidth <= 0 || limit == TimelineLimit.None)
			return (min, max);
		int index = Math.Clamp ((int)Math.Floor (x / columnWidth), 0, count - 1);
		return limit == TimelineLimit.Min
			? (Math.Min (index, max), max)
			: (min, Math.Max (index, min));
	}

	public static bool IsFull (int min, int max, int count)
		=> count <= 0 || (min <= 0 && max >= count - 1);
}
