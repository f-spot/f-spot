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

namespace FSpot.Utils
{
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

		public static Gdk.Rectangle TransformOrientation (Gdk.Pixbuf src, Gdk.Rectangle args, PixbufOrientation orientation)
		{
			return TransformOrientation (src.Width, src.Height, args, orientation);
		}
		
		public static Gdk.Rectangle TransformOrientation (int total_width, int total_height, Gdk.Rectangle args, PixbufOrientation orientation)
		{
			Gdk.Rectangle area = args;
			
			switch (orientation) {
			case PixbufOrientation.BottomRight:
				area.X = total_width - args.X - args.Width;
				area.Y = total_height - args.Y - args.Height;
				break;
			case PixbufOrientation.TopRight:
				area.X = total_width - args.X - args.Width;
				break;
			case PixbufOrientation.BottomLeft:
				area.Y = total_height - args.Y - args.Height;
				break;
			case PixbufOrientation.LeftTop:
				area.X = args.Y;
				area.Y = args.X;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case PixbufOrientation.RightBottom:
				area.X = total_height - args.Y - args.Height;
				area.Y = total_width - args.X - args.Width;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case PixbufOrientation.RightTop:
				area.X = total_height - args.Y - args.Height;
				area.Y = args.X;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case PixbufOrientation.LeftBottom:
				area.X = args.Y;
				area.Y = total_width - args.X - args.Width;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			default:
				break;
			}
			
			return area;
		}
	}
}
