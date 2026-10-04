//
// Histogram.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2004-2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using FSpot.Imaging;
using FSpot.Utils;

namespace FSpot
{
	public class Histogram
	{
		readonly byte[] colors = { 0x00, 0x00, 0x00, 0xff };

		public byte RedColorHint {
			set { colors[0] = value; }
		}

		public byte GreenColorHint {
			set { colors[1] = value; }
		}

		public byte BlueColorHint {
			set { colors[2] = value; }
		}

		public byte BackgroundColorHint {
			set { colors[3] = value; }
		}

		int[,] values = new int[256, 3];

		public Histogram (Gdk.Pixbuf src)
		{
			FillValues (src);
		}

		public Histogram () { }

		void FillValues (Gdk.Pixbuf src)
		{
			if (src.BitsPerSample != 8)
				throw new Exception ("Invalid bits per sample");

			values = RgbHistogram.CountRgb (src.ToImageBuffer ());
		}

		public void GetHighLow (int channel, out int high, out int low)
			=> RgbHistogram.GetHighLow (values, channel, out high, out low);

		public Gdk.Pixbuf Generate (Gdk.Pixbuf input, int max_width)
		{
			Gdk.Pixbuf scaled;
			using (Gdk.Pixbuf pixbuf = Generate (input))
				scaled = pixbuf.ScaleToMaxSize (max_width, 128);
			return scaled;
		}

		public Gdk.Pixbuf Generate (Gdk.Pixbuf input)
		{
			FillValues (input);
			const int height = 128;
			int width = values.GetLength (0);
			byte[] bgra = RgbHistogram.RenderBgra (values, width, height, colors[0], colors[1], colors[2], colors[3]);
			return ToPixbuf (bgra, width, height);
		}

		static Gdk.Pixbuf ToPixbuf (byte[] bgra, int width, int height)
		{
			var pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, width, height);
			int stride = pixbuf.Rowstride;
			var row = new byte[width * 4];
			for (int y = 0; y < height; y++) {
				int src = y * width * 4;
				for (int x = 0; x < width; x++) {
					int s = src + x * 4;
					int d = x * 4;
					row[d] = bgra[s + 2];
					row[d + 1] = bgra[s + 1];
					row[d + 2] = bgra[s];
					row[d + 3] = bgra[s + 3];
				}

				Marshal.Copy (row, 0, new IntPtr (pixbuf.Pixels.ToInt64 () + (long)y * stride), row.Length);
			}

			return pixbuf;
		}


#if FSPOT_HISTOGRAM_MAIN
		public static void Main (string [] args)
		{
			Gtk.Application.Init ();
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (args [0]);
			Logger.Log.DebugFormat ("loaded {0}", args [0]);
			Histogram hist = new Histogram ();
			Logger.Log.DebugFormat ("loaded histgram", args [0]);

			Gtk.Window win = new Gtk.Window ("display");
			Gtk.Image image = new Gtk.Image ();
			Gdk.Pixbuf img = hist.Generate (pixbuf);
			image.Pixbuf = img;
			win.Add (image);
			win.ShowAll ();
			Gtk.Application.Run ();

		}
#endif
	}
}
