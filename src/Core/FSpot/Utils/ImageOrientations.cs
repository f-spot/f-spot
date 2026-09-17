// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using TagLib.Image;

namespace FSpot.Utils
{
	public static class ImageOrientations
	{
		public static ImageOrientation Rotate270 (ImageOrientation orientation)
		{
			if (orientation == ImageOrientation.None)
				orientation = ImageOrientation.TopLeft;

			ImageOrientation[] rot = {
				ImageOrientation.LeftBottom,
				ImageOrientation.LeftTop,
				ImageOrientation.RightTop,
				ImageOrientation.RightBottom,
				ImageOrientation.BottomLeft,
				ImageOrientation.TopLeft,
				ImageOrientation.TopRight,
				ImageOrientation.BottomRight
			};

			return rot[((int)orientation) - 1];
		}

		public static ImageOrientation Rotate90 (ImageOrientation orientation)
			=> Rotate270 (Rotate270 (Rotate270 (orientation)));

		// GTK PixbufUtils.TransformOrientation: map a rectangle drawn on the
		// oriented display into the un-oriented buffer. Crop and red-eye use this;
		// pixelate/blackout apply the on-screen rect as-is.
		public static PixelRect TransformRect (int totalWidth, int totalHeight, PixelRect args, ImageOrientation orientation)
		{
			int x = args.X;
			int y = args.Y;
			int width = args.Width;
			int height = args.Height;

			switch (orientation) {
			case ImageOrientation.BottomRight:
				x = totalWidth - args.X - args.Width;
				y = totalHeight - args.Y - args.Height;
				break;
			case ImageOrientation.TopRight:
				x = totalWidth - args.X - args.Width;
				break;
			case ImageOrientation.BottomLeft:
				y = totalHeight - args.Y - args.Height;
				break;
			case ImageOrientation.LeftTop:
				x = args.Y;
				y = args.X;
				width = args.Height;
				height = args.Width;
				break;
			case ImageOrientation.RightBottom:
				x = totalHeight - args.Y - args.Height;
				y = totalWidth - args.X - args.Width;
				width = args.Height;
				height = args.Width;
				break;
			case ImageOrientation.RightTop:
				x = totalHeight - args.Y - args.Height;
				y = args.X;
				width = args.Height;
				height = args.Width;
				break;
			case ImageOrientation.LeftBottom:
				x = args.Y;
				y = totalWidth - args.X - args.Width;
				width = args.Height;
				height = args.Width;
				break;
			}

			return new PixelRect (x, y, width, height);
		}
	}
}
