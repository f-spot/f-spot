//
// GridGeometry.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;

namespace FSpot.Avalonia.Input;

/// <summary>
/// Cell math for the uniform photo grid, in content coordinates (scroll offset included).
/// </summary>
public static class GridGeometry
{
	/// <summary>
	/// GTK <c>CellGridView.CellsInRect</c>: every cell the rubber band touches.
	/// </summary>
	public static IReadOnlyList<int> CellsInRect (Rect band, double cellWidth, double cellHeight, int columns, int count)
	{
		var cells = new List<int> ();
		if (count <= 0 || cellWidth <= 0 || cellHeight <= 0 || columns <= 0 || band.Width < 0 || band.Height < 0)
			return cells;

		int rows = (count + columns - 1) / columns;
		int firstColumn = Math.Max (0, (int)Math.Floor (band.Left / cellWidth));
		int lastColumn = Math.Min (columns - 1, (int)Math.Floor (band.Right / cellWidth));
		int firstRow = Math.Max (0, (int)Math.Floor (band.Top / cellHeight));
		int lastRow = Math.Min (rows - 1, (int)Math.Floor (band.Bottom / cellHeight));
		for (int row = firstRow; row <= lastRow; row++) {
			for (int column = firstColumn; column <= lastColumn; column++) {
				int index = row * columns + column;
				if (index < count)
					cells.Add (index);
			}
		}

		return cells;
	}

	/// <summary>
	/// GTK <c>BoundedRectangle</c>: the box between the press point and the pointer.
	/// </summary>
	public static Rect Bounded (Point a, Point b)
		=> new (Math.Min (a.X, b.X), Math.Min (a.Y, b.Y), Math.Abs (a.X - b.X), Math.Abs (a.Y - b.Y));

	/// <summary>
	/// GTK <c>UpdateRubberbandSelection</c>: start from the selection at press time, then add
	/// each banded cell, or toggle it when Ctrl is held.
	/// </summary>
	public static HashSet<int> BandSelection (IReadOnlySet<int> initial, IReadOnlyList<int> banded, bool toggle)
	{
		var selection = new HashSet<int> (initial);
		foreach (int cell in banded) {
			if (!toggle)
				selection.Add (cell);
			else if (!selection.Remove (cell))
				selection.Add (cell);
		}

		return selection;
	}
}
