//
// TimelineLayout.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Controls;

/// <summary>
/// GTK GroupSelector in Min mode: a month is at least <paramref name="minColumn"/> wide,
/// and months stretch to fill the strip when there is room.
/// </summary>
public static class TimelineLayout
{
	public const double StripHeight = 48;
	public const double HandleHeight = 14;
	public const double LegendHeight = 13;
	public const double ArrowWidth = 16;

	public readonly record struct Chrome (double Legend, double Bars, double Handle);

	public readonly record struct MeasureResult (double ColumnWidth, double ContentWidth, bool Scrolls);

	/// <summary>
	/// GTK GroupSelector: a short chart, a year line at the top of it, and the glass handle underneath.
	/// </summary>
	public static Chrome Split (double height)
	{
		double available = Math.Max (HandleHeight + LegendHeight + 8, height);
		double handle = Math.Min (HandleHeight, available / 3);
		double legend = Math.Min (LegendHeight, (available - handle) * 0.4);
		double bars = available - legend - handle;
		return new Chrome (legend, bars, handle);
	}

	public static MeasureResult Measure (int count, double viewportWidth, double minColumn)
	{
		if (count <= 0)
			return new MeasureResult (minColumn, 0, false);
		double viewport = Math.Max (0, viewportWidth);
		double floor = Math.Max (1, minColumn);
		double column = Math.Max (floor, viewport / count);
		double content = column * count;
		return new MeasureResult (column, content, content > viewport + 0.5);
	}

	public static int IndexAt (double x, double columnWidth, int count)
	{
		if (count <= 0 || columnWidth <= 0 || x < 0)
			return -1;
		int index = (int)Math.Floor (x / columnWidth);
		return index >= count ? -1 : index;
	}

	public static (double X, double Width) Span (int start, int end, double columnWidth)
	{
		int first = Math.Min (start, end);
		int last = Math.Max (start, end);
		return (first * columnWidth, (last - first + 1) * columnWidth);
	}

	/// <summary>
	/// GTK <c>BoxXHitFilled</c>: an empty month snaps to the nearest month that has photos.
	/// A tie prefers the earlier month. Returns -1 when every month is empty.
	/// </summary>
	public static int NearestFilled (IReadOnlyList<bool> filled, int index)
	{
		if (filled.Count == 0)
			return -1;
		index = Math.Clamp (index, 0, filled.Count - 1);
		if (filled[index])
			return index;

		int upper = index;
		while (upper < filled.Count && !filled[upper])
			upper++;
		int lower = index;
		while (lower >= 0 && !filled[lower])
			lower--;

		if (lower < 0 && upper >= filled.Count)
			return -1;
		if (lower < 0)
			return upper;
		if (upper >= filled.Count)
			return lower;
		return upper + 1 - index > index - lower ? lower : upper;
	}
}
