//
// LoupeDrag.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Utils;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>Loupe.DragUpdate</c> and <c>MoveWindow</c>: a plain drag moves the hotspot with
/// the pointer; a Shift drag turns the loupe around its hotspot.
/// </summary>
public static class LoupeDrag
{
	/// <summary>GTK: any button other than the first turns the loupe by an eighth of a half turn.</summary>
	public const double ClickTurn = Math.PI / 8;

	public static (double X, double Y) Move (double startHotX, double startHotY,
		double startX, double startY, double nowX, double nowY)
		=> (startHotX + nowX - startX, startHotY + nowY - startY);

	public static double Rotate (double startAngle, double hotX, double hotY,
		double startX, double startY, double nowX, double nowY)
	{
		double ix = startX - hotX;
		double iy = startY - hotY;
		double nx = nowX - hotX;
		double ny = nowY - hotY;
		if ((ix == 0 && iy == 0) || (nx == 0 && ny == 0))
			return startAngle;
		double between = Vector.AngleBetween (new Vector (ix, iy), new Vector (nx, ny));
		return double.IsNaN (between) ? startAngle : startAngle + between;
	}
}
