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

		public static Rectangle TransformOrientation (Pixbuf src, Rectangle args, PixbufOrientation orientation)
		{
			return TransformOrientation (src.Width, src.Height, args, orientation);
		}
		
		public static Rectangle TransformOrientation (int total_width, int total_height, Rectangle args, PixbufOrientation orientation)
		{
			Rectangle area = args;
			
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

		public static Point TransformOrientation (int total_width, int total_height, Point args, PixbufOrientation orientation)
		{
			Point p = args;

			switch (orientation) {
			default:
			case PixbufOrientation.TopLeft:
				break;
			case PixbufOrientation.TopRight:
				p.X = total_width - p.X;
				break;
			case PixbufOrientation.BottomRight:
				p.X = total_width - p.X;
				p.Y = total_height - p.Y;
				break;
			case PixbufOrientation.BottomLeft:
				p.Y = total_height - p.Y;
				break;
			case PixbufOrientation.LeftTop:
				p.X = args.Y;
				p.Y = args.X;
				break;
			case PixbufOrientation.RightTop:
				p.X = total_height - args.Y;
				p.Y = args.X;
				break;
			case PixbufOrientation.RightBottom:
				p.X = total_height - args.Y;
				p.Y = total_width - args.X;
				break;
			case PixbufOrientation.LeftBottom:
				p.X = args.Y;
				p.Y = total_width - args.X;
				break;
			}
			return p;
		}

		public static PixbufOrientation ReverseTransformation (PixbufOrientation orientation)
		{
			switch (orientation) {
			default:
			case PixbufOrientation.TopLeft:
			case PixbufOrientation.TopRight:
			case PixbufOrientation.BottomRight:
			case PixbufOrientation.BottomLeft:
				return orientation;
			case PixbufOrientation.LeftTop:
				return PixbufOrientation.RightBottom;
			case PixbufOrientation.RightTop:
				return PixbufOrientation.LeftBottom;
			case PixbufOrientation.RightBottom:
				return PixbufOrientation.LeftTop;
			case PixbufOrientation.LeftBottom:
				return PixbufOrientation.RightTop;
			}
		}

		public static Pixbuf TransformOrientation (Pixbuf src, PixbufOrientation orientation)
		{
			Pixbuf dest;

			switch (orientation) {
			default:
			case PixbufOrientation.TopLeft:
				dest = PixbufUtils.ShallowCopy (src);
				break;
			case PixbufOrientation.TopRight:
				dest = src.Flip (false);
				break;
			case PixbufOrientation.BottomRight:
				dest = src.RotateSimple (PixbufRotation.Upsidedown);
				break;
			case PixbufOrientation.BottomLeft:
				dest = src.Flip (true);
				break;
			case PixbufOrientation.LeftTop:
				using (var rotated = src.RotateSimple (PixbufRotation.Clockwise)) {
					dest = rotated.Flip (false);
				}
				break;
			case PixbufOrientation.RightTop:
				dest = src.RotateSimple (PixbufRotation.Clockwise);
				break;
			case PixbufOrientation.RightBottom:
				using (var rotated = src.RotateSimple (PixbufRotation.Counterclockwise)) {
					dest = rotated.Flip (false);
				}
				break;
			case PixbufOrientation.LeftBottom:
				dest = src.RotateSimple (PixbufRotation.Counterclockwise);
				break;
			}
			
			return dest;
		}

		public static Pixbuf ShallowCopy (Pixbuf pixbuf)
		{
			if (pixbuf == null)
				return null;
			Pixbuf result = new Pixbuf (pixbuf, 0, 0, pixbuf.Width, pixbuf.Height);
			return result;
		}
	}
}
