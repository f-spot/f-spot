//
// FSpot.PixbufUtils.cs
//
// Author(s):
//	Ettore Perazzoli
//	Larry Ewing  <lewing@novell.com>
//	Stephane Delcroix  <stephane@declroix.org>
//
// This is free software. See COPYING for details
//

using Gdk;
using System;
using System.Runtime.InteropServices;
using TagLib.Image;

namespace FSpot.Utils
{
	public static class PixbufUtils
	{
        static public ImageOrientation Rotate270 (ImageOrientation orientation)
        {
            if (orientation == ImageOrientation.None) {
                orientation = ImageOrientation.TopLeft;
            }

            ImageOrientation [] rot = new ImageOrientation [] {
                ImageOrientation.LeftBottom,
                    ImageOrientation.LeftTop,
                    ImageOrientation.RightTop,
                    ImageOrientation.RightBottom,
                    ImageOrientation.BottomLeft,
                    ImageOrientation.TopLeft,
                    ImageOrientation.TopRight,
                    ImageOrientation.BottomRight
            };

            orientation = rot [((int)orientation) -1];
            return orientation;
        }
	
		static public ImageOrientation Rotate90 (ImageOrientation orientation)
		{
			orientation = Rotate270 (orientation);
			orientation = Rotate270 (orientation);
			orientation = Rotate270 (orientation);
			return orientation;
		}

		public static Rectangle TransformOrientation (Pixbuf src, Rectangle args, ImageOrientation orientation)
		{
			return TransformOrientation (src.Width, src.Height, args, orientation);
		}
		
		public static Rectangle TransformOrientation (int total_width, int total_height, Rectangle args, ImageOrientation orientation)
		{
			Rectangle area = args;
			
			switch (orientation) {
			case ImageOrientation.BottomRight:
				area.X = total_width - args.X - args.Width;
				area.Y = total_height - args.Y - args.Height;
				break;
			case ImageOrientation.TopRight:
				area.X = total_width - args.X - args.Width;
				break;
			case ImageOrientation.BottomLeft:
				area.Y = total_height - args.Y - args.Height;
				break;
			case ImageOrientation.LeftTop:
				area.X = args.Y;
				area.Y = args.X;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case ImageOrientation.RightBottom:
				area.X = total_height - args.Y - args.Height;
				area.Y = total_width - args.X - args.Width;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case ImageOrientation.RightTop:
				area.X = total_height - args.Y - args.Height;
				area.Y = args.X;
				area.Width = args.Height;
				area.Height = args.Width;
				break;
			case ImageOrientation.LeftBottom:
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

		public static Point TransformOrientation (int total_width, int total_height, Point args, ImageOrientation orientation)
		{
			Point p = args;

			switch (orientation) {
			default:
			case ImageOrientation.TopLeft:
				break;
			case ImageOrientation.TopRight:
				p.X = total_width - p.X;
				break;
			case ImageOrientation.BottomRight:
				p.X = total_width - p.X;
				p.Y = total_height - p.Y;
				break;
			case ImageOrientation.BottomLeft:
				p.Y = total_height - p.Y;
				break;
			case ImageOrientation.LeftTop:
				p.X = args.Y;
				p.Y = args.X;
				break;
			case ImageOrientation.RightTop:
				p.X = total_height - args.Y;
				p.Y = args.X;
				break;
			case ImageOrientation.RightBottom:
				p.X = total_height - args.Y;
				p.Y = total_width - args.X;
				break;
			case ImageOrientation.LeftBottom:
				p.X = args.Y;
				p.Y = total_width - args.X;
				break;
			}
			return p;
		}

		public static ImageOrientation ReverseTransformation (ImageOrientation orientation)
		{
			switch (orientation) {
			default:
			case ImageOrientation.TopLeft:
			case ImageOrientation.TopRight:
			case ImageOrientation.BottomRight:
			case ImageOrientation.BottomLeft:
				return orientation;
			case ImageOrientation.LeftTop:
				return ImageOrientation.RightBottom;
			case ImageOrientation.RightTop:
				return ImageOrientation.LeftBottom;
			case ImageOrientation.RightBottom:
				return ImageOrientation.LeftTop;
			case ImageOrientation.LeftBottom:
				return ImageOrientation.RightTop;
			}
		}

		public static Pixbuf TransformOrientation (Pixbuf src, ImageOrientation orientation)
		{
			Pixbuf dest;

			switch (orientation) {
			default:
			case ImageOrientation.TopLeft:
				dest = PixbufUtils.ShallowCopy (src);
				break;
			case ImageOrientation.TopRight:
				dest = src.Flip (false);
				break;
			case ImageOrientation.BottomRight:
				dest = src.RotateSimple (PixbufRotation.Upsidedown);
				break;
			case ImageOrientation.BottomLeft:
				dest = src.Flip (true);
				break;
			case ImageOrientation.LeftTop:
				using (var rotated = src.RotateSimple (PixbufRotation.Clockwise)) {
					dest = rotated.Flip (false);
				}
				break;
			case ImageOrientation.RightTop:
				dest = src.RotateSimple (PixbufRotation.Clockwise);
				break;
			case ImageOrientation.RightBottom:
				using (var rotated = src.RotateSimple (PixbufRotation.Counterclockwise)) {
					dest = rotated.Flip (false);
				}
				break;
			case ImageOrientation.LeftBottom:
				dest = src.RotateSimple (PixbufRotation.Counterclockwise);
				break;
			}
			
			return dest;
		}

		public static Pixbuf ShallowCopy (this Pixbuf pixbuf)
		{
			if (pixbuf == null)
				return null;
			Pixbuf result = new Pixbuf (pixbuf, 0, 0, pixbuf.Width, pixbuf.Height);
			return result;
		}
	}
}
