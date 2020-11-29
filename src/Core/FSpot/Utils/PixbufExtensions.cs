//
// PixbufExtensions.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Gdk;

using Hyena;

using TagLib.Image;

namespace FSpot.Utils
{
	// These are also in FSpot.Gtk.
	static class PixbufExtensions
	{
		public static Pixbuf ShallowCopy (this Pixbuf pixbuf)
		{
			if (pixbuf == null)
				return null;

			var result = new Pixbuf (pixbuf, 0, 0, pixbuf.Width, pixbuf.Height);
			return result;
		}

		public static Pixbuf ScaleToMaxSize (this Pixbuf pixbuf, int width, int height, bool upscale = true)
		{
			double scale = Fit (pixbuf, width, height, upscale, out var scaleWidth, out var scaleHeight);

			Pixbuf result;
			if (upscale || (scale < 1.0))
				result = pixbuf.ScaleSimple (scaleWidth, scaleHeight,
					(scaleWidth > 20) ? InterpType.Bilinear : InterpType.Nearest);
			else
				result = pixbuf.Copy ();

			return result;
		}

		public static double Fit (this Pixbuf pixbuf, int destWidth, int destHeight, bool upscaleSmaller, out int fitWidth, out int fitHeight)
		{
			return Fit (pixbuf.Width, pixbuf.Height, destWidth, destHeight, upscaleSmaller, out fitWidth, out fitHeight);
		}

		public static double Fit (int origWidth, int origHeight, int destWidth, int destHeight, bool upscaleSmaller, out int fitWidth, out int fitHeight)
		{
			if (origWidth == 0 || origHeight == 0) {
				fitWidth = 0;
				fitHeight = 0;
				return 0.0;
			}

			double scale = Math.Min (destWidth / (double)origWidth, destHeight / (double)origHeight);

			if (scale > 1.0 && !upscaleSmaller)
				scale = 1.0;

			fitWidth = (int)Math.Round (scale * origWidth);
			fitHeight = (int)Math.Round (scale * origHeight);

			return scale;
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

		public static void CreateDerivedVersion (SafeUri source, SafeUri destination, uint jpegQuality, Pixbuf pixbuf)
		{
			SaveToSuitableFormat (destination, pixbuf, jpegQuality);

			using var metadataFrom = MetadataUtils.Parse (source);
			using var metadataTo = MetadataUtils.Parse (destination);

			metadataTo.CopyFrom (metadataFrom);

			// Reset orientation to make sure images appear upright.
			metadataTo.ImageTag.Orientation = ImageOrientation.TopLeft;
			metadataTo.Save ();
		}

		static void SaveToSuitableFormat (SafeUri destination, Pixbuf pixbuf, uint jpegQuality)
		{
			// FIXME: this needs to work on streams rather than filenames. Do that when we switch to
			// newer GDK.
			var extension = destination.GetExtension ().ToLower ();
			if (extension == ".png")
				pixbuf.Save (destination.LocalPath, "png");
			else if (extension == ".jpg" || extension == ".jpeg")
				pixbuf.Save (destination.LocalPath, "jpeg", jpegQuality);
			else
				throw new NotImplementedException ("Saving this file format is not supported");
		}

		#region Gdk hackery

		// This hack below is needed because there is no wrapped version of
		// Save which allows specifying the variable arguments (it's not
		// possible with p/invoke).

		[DllImport ("libgdk_pixbuf-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_pixbuf_save (IntPtr raw, IntPtr filename, IntPtr type, out IntPtr error,
			IntPtr optlabel1, IntPtr optvalue1, IntPtr dummy);

		static bool Save (this Pixbuf pixbuf, string filename, string type, uint jpegQuality)
		{
			IntPtr nfilename = GLib.Marshaller.StringToPtrGStrdup (filename);
			IntPtr ntype = GLib.Marshaller.StringToPtrGStrdup (type);
			IntPtr optlabel1 = GLib.Marshaller.StringToPtrGStrdup ("quality");
			IntPtr optvalue1 = GLib.Marshaller.StringToPtrGStrdup (jpegQuality.ToString ());
			bool ret = gdk_pixbuf_save (pixbuf.Handle, nfilename, ntype, out var error, optlabel1, optvalue1,
				IntPtr.Zero);
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
