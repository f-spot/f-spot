//
// ThumbnailWindow.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

/// <summary>
/// Indexes of a uniform grid that should hold a decoded thumbnail.
/// The count tracks the viewport plus a few rows of overscan, never the catalog.
/// </summary>
public static class ThumbnailWindow
{
	public readonly record struct Slice (int First, int Count)
	{
		public static Slice Empty { get; } = new (0, 0);
	}

	public static Slice Range (
		int itemCount,
		int columns,
		double scrollOffset,
		double viewport,
		double itemExtent,
		int overscanRows)
	{
		if (itemCount <= 0 || viewport <= 0 || itemExtent <= 0 || double.IsNaN (viewport) || double.IsInfinity (viewport))
			return Slice.Empty;
		if (columns < 1)
			columns = 1;
		if (overscanRows < 0)
			overscanRows = 0;
		if (scrollOffset < 0 || double.IsNaN (scrollOffset))
			scrollOffset = 0;

		int rowCount = (itemCount + columns - 1) / columns;
		int firstVisible = (int)Math.Floor (scrollOffset / itemExtent);
		if (firstVisible >= rowCount)
			firstVisible = rowCount - 1;
		int lastExclusive = (int)Math.Ceiling ((scrollOffset + viewport) / itemExtent);
		if (lastExclusive < firstVisible + 1)
			lastExclusive = firstVisible + 1;

		int startRow = Math.Max (0, firstVisible - overscanRows);
		int endRow = Math.Min (rowCount, lastExclusive + overscanRows);
		int first = startRow * columns;
		int end = Math.Min (itemCount, endRow * columns);
		return end <= first ? Slice.Empty : new Slice (first, end - first);
	}
}
