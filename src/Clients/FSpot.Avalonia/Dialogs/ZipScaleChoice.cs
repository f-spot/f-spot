// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Dialogs;

// GTK zip export: the scale box starts off, and the edge spinner is 1..10000, default 1024.
public readonly record struct ZipScaleChoice (bool Scale, int LongerSide)
{
	public const int DefaultLongerSide = 1024;
	public const int MinimumLongerSide = 1;
	public const int MaximumLongerSide = 10000;
}
