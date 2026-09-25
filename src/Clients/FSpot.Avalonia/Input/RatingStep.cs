//
// RatingStep.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>RatingEntry.OnKeyPressEvent</c> and <c>HandleScroll</c>: Up, Right, + and = raise
/// the rating; Down, Left and - lower it; the wheel does the same. The value stays in 0..5.
/// </summary>
public static class RatingStep
{
	public const uint MaxRating = 5;

	public static int KeyDelta (Key key)
		=> key switch {
			Key.Up or Key.Right or Key.OemPlus or Key.Add => 1,
			Key.Down or Key.Left or Key.OemMinus or Key.Subtract => -1,
			_ => 0
		};

	/// <summary>Wheel up or right raises, down or left lowers.</summary>
	public static int WheelDelta (double deltaX, double deltaY)
	{
		double delta = Math.Abs (deltaY) >= Math.Abs (deltaX) ? deltaY : deltaX;
		return delta > 0 ? 1 : delta < 0 ? -1 : 0;
	}

	public static uint Apply (uint current, int delta)
	{
		long next = (long)Math.Min (current, MaxRating) + delta;
		return (uint)Math.Clamp (next, 0, MaxRating);
	}
}
