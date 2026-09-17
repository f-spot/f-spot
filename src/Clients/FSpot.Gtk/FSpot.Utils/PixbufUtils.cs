//
// PixbufUtils.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using FSpot.Imaging;

using Gdk;

using Hyena;

using TagLib.Image;

namespace FSpot.Utils
{
	public static class PixbufUtils
	{
		public static ImageBuffer ToImageBuffer (this Pixbuf pixbuf)
		{
			if (pixbuf == null)
				return null;
			int length = pixbuf.Rowstride * pixbuf.Height;
			var pixels = new byte[length];
			Marshal.Copy (pixbuf.Pixels, pixels, 0, length);
			return new ImageBuffer (pixbuf.Width, pixbuf.Height, pixbuf.HasAlpha, pixbuf.Rowstride, pixels);
		}

		public static Pixbuf ToPixbuf (this ImageBuffer buffer)
		{
			if (buffer == null)
				return null;
			using var view = new Pixbuf (buffer.Pixels, Colorspace.Rgb, buffer.HasAlpha, 8, buffer.Width, buffer.Height, buffer.Rowstride);
			return view.Copy ();
		}

		public static ImageOrientation Rotate270 (ImageOrientation orientation)
			=> ImageOrientations.Rotate270 (orientation);

		public static ImageOrientation Rotate90 (ImageOrientation orientation)
			=> ImageOrientations.Rotate90 (orientation);

		public static Rectangle TransformOrientation (Pixbuf src, Rectangle args, ImageOrientation orientation)
		{
			return TransformOrientation (src.Width, src.Height, args, orientation);
		}

		public static Rectangle TransformOrientation (int total_width, int total_height, Rectangle args, ImageOrientation orientation)
		{
			var core = ImageOrientations.TransformRect (total_width, total_height,
				new PixelRect (args.X, args.Y, args.Width, args.Height), orientation);
			return new Rectangle (core.X, core.Y, core.Width, core.Height);
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

		public static Pixbuf TransformOrientation (this Pixbuf src, ImageOrientation orientation)
		{
			Pixbuf dest;

			switch (orientation) {
			default:
			case ImageOrientation.TopLeft:
				dest = src.ShallowCopy ();
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

			var result = new Pixbuf (pixbuf, 0, 0, pixbuf.Width, pixbuf.Height);
			return result;
		}

		public static Pixbuf ScaleToMaxSize (this Pixbuf pixbuf, int width, int height, bool upscale = true)
		{
			double scale = Fit (pixbuf, width, height, upscale, out var scale_width, out var scale_height);

			Gdk.Pixbuf result;
			if (upscale || (scale < 1.0))
				result = pixbuf.ScaleSimple (scale_width, scale_height, (scale_width > 20) ? Gdk.InterpType.Bilinear : Gdk.InterpType.Nearest);
			else
				result = pixbuf.Copy ();

			return result;
		}

		public static double Fit (Pixbuf pixbuf, int dest_width, int dest_height, bool upscale_smaller, out int fit_width, out int fit_height)
		{
			return Fit (pixbuf.Width, pixbuf.Height,
				dest_width, dest_height,
				upscale_smaller,
				out fit_width, out fit_height);
		}

		public static double Fit (int orig_width, int orig_height, int dest_width, int dest_height, bool upscale_smaller, out int fit_width, out int fit_height)
		{
			if (orig_width == 0 || orig_height == 0) {
				fit_width = 0;
				fit_height = 0;
				return 0.0;
			}

			double scale = Math.Min (dest_width / (double)orig_width,
				dest_height / (double)orig_height);

			if (scale > 1.0 && !upscale_smaller)
				scale = 1.0;

			fit_width = (int)Math.Round (scale * orig_width);
			fit_height = (int)Math.Round (scale * orig_height);

			return scale;
		}

		public static void CreateDerivedVersion (SafeUri source, SafeUri destination, uint jpeg_quality, Pixbuf pixbuf)
		{
			DerivedVersionFile.Publish (source, destination, temp => {
				SaveToSuitableFormat (new SafeUri (temp), pixbuf, jpeg_quality);
			});
		}

		static void SaveToSuitableFormat (SafeUri destination, Pixbuf pixbuf, uint jpeg_quality)
		{
			// FIXME: this needs to work on streams rather than filenames. Do that when we switch to
			// newer GDK.
			var extension = destination.GetExtension ().ToLower ();
			if (extension == ".png")
				pixbuf.Save (destination.LocalPath, "png");
			else if (extension == ".jpg" || extension == ".jpeg")
				pixbuf.Save (destination.LocalPath, "jpeg", jpeg_quality);
			else
				throw new NotImplementedException ("Saving this file format is not supported");
		}

		#region Gdk hackery

		// This hack below is needed because there is no wrapped version of
		// Save which allows specifying the variable arguments (it's not
		// possible with p/invoke).

		[DllImport ("libgdk_pixbuf-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_pixbuf_save (IntPtr raw, IntPtr filename, IntPtr type, out IntPtr error, IntPtr optlabel1, IntPtr optvalue1, IntPtr dummy);

		static bool Save (this Pixbuf pixbuf, string filename, string type, uint jpeg_quality)
		{
			IntPtr error = IntPtr.Zero;
			IntPtr nfilename = GLib.Marshaller.StringToPtrGStrdup (filename);
			IntPtr ntype = GLib.Marshaller.StringToPtrGStrdup (type);
			IntPtr optlabel1 = GLib.Marshaller.StringToPtrGStrdup ("quality");
			IntPtr optvalue1 = GLib.Marshaller.StringToPtrGStrdup (jpeg_quality.ToString ());
			bool ret = gdk_pixbuf_save (pixbuf.Handle, nfilename, ntype, out error, optlabel1, optvalue1, IntPtr.Zero);
			GLib.Marshaller.Free (nfilename);
			GLib.Marshaller.Free (ntype);
			GLib.Marshaller.Free (optlabel1);
			GLib.Marshaller.Free (optvalue1);
			if (error != IntPtr.Zero)
				throw new GLib.GException (error);
			return ret;
		}

		#endregion
	}
}
