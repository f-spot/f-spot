// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Imaging
{
	public readonly struct PixelRect
	{
		public PixelRect (int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public int X { get; }
		public int Y { get; }
		public int Width { get; }
		public int Height { get; }
		public bool IsEmpty => Width <= 0 || Height <= 0;
	}
}
