//
// FSpot.PixbufUtils.cs
//
// Author(s):
//	Ettore Perazzoli
//	Larry Ewing  <lewing@novell.com>
//	Stephane Delcroix  <stephane@declroix.org>
//
// This is free softwae. See cOPYING for details
//

/**
  1        2       3      4         5            6           7          8

888888  888888      88  88      8888888888  88                  88  8888888888
88          88      88  88      88  88      88  88          88  88      88  88
8888      8888    8888  8888    88          8888888888  8888888888          88
88          88      88  88
88          88  888888  888888

t-l     t-r     b-r     b-l     l-t         r-t         r-b             l-b

**/

namespace FSpot.Utils
{
	public enum PixbufOrientation {
		TopLeft = 1,
		TopRight = 2,
		BottomRight = 3,
		BottomLeft = 4,
		LeftTop = 5,
		RightTop = 6,
		RightBottom = 7,
		LeftBottom = 8
	}

	public static class PixbufUtils
	{
		static public PixbufOrientation Rotate270 (PixbufOrientation orientation)
		{
			PixbufOrientation [] rot = new PixbufOrientation [] {
				PixbufOrientation.LeftBottom, 
				PixbufOrientation.LeftTop,
				PixbufOrientation.RightTop,
				PixbufOrientation.RightBottom, 
				PixbufOrientation.BottomLeft,
				PixbufOrientation.TopLeft,
				PixbufOrientation.TopRight,
				PixbufOrientation.BottomRight
			};
	
			orientation = rot [((int)orientation) -1];
			return orientation;
		}
	
		static public PixbufOrientation Rotate90 (PixbufOrientation orientation)
		{
			orientation = Rotate270 (orientation);
			orientation = Rotate270 (orientation);
			orientation = Rotate270 (orientation);
			return orientation;
		}
	}
}
