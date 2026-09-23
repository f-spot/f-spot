// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;

namespace FSpot.Avalonia.Input;

public static class DragGesture
{
	public const double Threshold = 6;

	public static bool MovedFarEnough (Point start, Point current)
		=> Math.Abs (current.X - start.X) >= Threshold || Math.Abs (current.Y - start.Y) >= Threshold;
}
