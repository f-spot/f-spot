//
// SkiaOrient.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using SkiaSharp;

using TagLib.Image;

namespace FSpot.Avalonia.Imaging;

// The same turns as ImageBuffers.TransformOrientation, drawn by Skia.
// A rotated phone photo was spending the decode gate on a C# pixel walk.
static class SkiaOrient
{
	internal static SKBitmap Apply (SKBitmap bitmap, ImageOrientation orientation)
	{
		if (bitmap is null)
			throw new ArgumentNullException (nameof (bitmap));

		SKBitmap current = bitmap;
		switch (orientation) {
		case ImageOrientation.TopRight:
			Step (FlipVertical (current));
			break;
		case ImageOrientation.BottomRight:
			Step (FlipVertical (current));
			Step (FlipHorizontal (current));
			break;
		case ImageOrientation.BottomLeft:
			Step (FlipHorizontal (current));
			break;
		case ImageOrientation.LeftTop:
			Step (Rotate90Clockwise (current));
			Step (FlipVertical (current));
			break;
		case ImageOrientation.RightTop:
			Step (Rotate90Clockwise (current));
			break;
		case ImageOrientation.RightBottom:
			Step (Rotate90Counterclockwise (current));
			Step (FlipVertical (current));
			break;
		case ImageOrientation.LeftBottom:
			Step (Rotate90Counterclockwise (current));
			break;
		default:
			return bitmap;
		}

		if (!ReferenceEquals (current, bitmap))
			bitmap.Dispose ();
		return current;

		void Step (SKBitmap next)
		{
			if (!ReferenceEquals (current, bitmap))
				current.Dispose ();
			current = next;
		}
	}

	static SKBitmap FlipVertical (SKBitmap bitmap)
	{
		var destination = New (bitmap, bitmap.Width, bitmap.Height);
		using var canvas = new SKCanvas (destination);
		canvas.Scale (1, -1);
		canvas.Translate (0, -bitmap.Height);
		canvas.DrawBitmap (bitmap, 0, 0, new SKSamplingOptions (SKFilterMode.Nearest));
		return destination;
	}

	static SKBitmap FlipHorizontal (SKBitmap bitmap)
	{
		var destination = New (bitmap, bitmap.Width, bitmap.Height);
		using var canvas = new SKCanvas (destination);
		canvas.Scale (-1, 1);
		canvas.Translate (-bitmap.Width, 0);
		canvas.DrawBitmap (bitmap, 0, 0, new SKSamplingOptions (SKFilterMode.Nearest));
		return destination;
	}

	static SKBitmap Rotate90Clockwise (SKBitmap bitmap)
	{
		var destination = New (bitmap, bitmap.Height, bitmap.Width);
		using var canvas = new SKCanvas (destination);
		canvas.Translate (bitmap.Height, 0);
		canvas.RotateDegrees (90);
		canvas.DrawBitmap (bitmap, 0, 0, new SKSamplingOptions (SKFilterMode.Nearest));
		return destination;
	}

	static SKBitmap Rotate90Counterclockwise (SKBitmap bitmap)
	{
		var destination = New (bitmap, bitmap.Height, bitmap.Width);
		using var canvas = new SKCanvas (destination);
		canvas.Translate (0, bitmap.Width);
		canvas.RotateDegrees (-90);
		canvas.DrawBitmap (bitmap, 0, 0, new SKSamplingOptions (SKFilterMode.Nearest));
		return destination;
	}

	static SKBitmap New (SKBitmap bitmap, int width, int height)
		=> new (new SKImageInfo (width, height, bitmap.ColorType, bitmap.AlphaType));
}
