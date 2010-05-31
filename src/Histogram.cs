/* 
 * FSpot.Histogram.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Ruben Vermeersch  <ruben@savanne.be>
 *
 * This is free software, See COPYING for details.
 */

using System;

namespace FSpot {
	public class Histogram {
#region Color hints
		private byte [] colors = new byte [] {0x00, 0x00, 0x00, 0xff};

		public byte RedColorHint {
			set { colors [0] = value; }
		}

		public byte GreenColorHint {
			set { colors [1] = value; }
		}

		public byte BlueColorHint {
			set { colors [2] = value; }
		}

		public byte BackgroundColorHint {
			set { colors [3] = value; }
		}

		private int [,] values = new int [256, 3];
#endregion

		public Histogram (Gdk.Pixbuf src)
		{
		        FillValues (src);
		}
		
		public Histogram () {}

		private void FillValues (Gdk.Pixbuf src)
		{
			values = new int [256, 3];

			if (src.BitsPerSample != 8)
				throw new System.Exception ("Invalid bits per sample");
						
			unsafe {
				byte * srcb = (byte *)src.Pixels;
				byte * pixels = srcb;
				bool alpha = src.HasAlpha;
				int rowstride = src.Rowstride;
				int width = src.Width;
				int height = src.Height;

				// FIXME array bounds checks slow this down a lot
				// so we use a pointer.  It is sad but I want fastness
				fixed (int * v = &values [0,0]) {
					for (int j = 0; j < height; j++) {
						for (int i = 0; i < width; i++) {
							v [*(srcb++) * 3 + 0]++;
							v [*(srcb++) * 3 + 1]++;
							v [*(srcb++) * 3 + 2]++;
							
							if (alpha)
								srcb++;
							
						}
						srcb =  ((byte *) pixels) + j * rowstride;
					}
				}
			}
		}
		
		private int ChannelSum (int channel)
		{
			int sum = 0;
			for (int i = 0; i < values.GetLength (0); i++) {
				sum += values [i, channel];
			}

			return sum;
		}

		public void GetHighLow (int channel, out int high, out int low)
		{
			double total = ChannelSum (channel);
			double current = 0.0;
			double percentage;
			double next_percentage;
			
			low = 0;
			high = 0;
			
			for (int i = 0; i < values.GetLength (0) - 1; i++) {
				current += values [i, channel];
				percentage = current / total;
				next_percentage = (current + values [i + 1, channel]) / total;
				if (Math.Abs (percentage - 0.006) < Math.Abs (next_percentage - 0.006)) {
					low = i + 1;
					break;
				}
			}

			for (int i = values.GetLength (0) - 1; i > 0; i--) {
				current += values [i, channel];
				percentage = current / total;
				next_percentage = (current + values [i - 1, channel]) / total;
				if (Math.Abs (percentage - 0.006) < Math.Abs (next_percentage - 0.006)) {
					high = i - 1;
					break;
				}
			}
		}

		private void Draw (Gdk.Pixbuf image) 
		{
			int max = 0;
			for (int i = 0; i < values.GetLength (0); i++) {
				for (int j = 0; j < values.GetLength (1); j++) {
					max = System.Math.Max (max, values [i, j]);
				}
			}
			unsafe {
				int height = image.Height;
				int rowstride = image.Rowstride;
				int r = 0;
				int b = 0;
				int g = 0;
				
				for (int i = 0; i < image.Width; i++) {
					byte * pixels = (byte *)image.Pixels + i * 4;
					
					if (max > 0) {
						r = values [i, 0] * height / max;
						g = values [i, 1] * height / max;
						b = values [i, 2] * height / max;
					} else 
						r = g = b = 0;

					int top = Math.Max (r, Math.Max (g, b));

					int j = 0;
					for (; j < height - top; j++) {
						pixels [0] = colors [0];
						pixels [1] = colors [1];
						pixels [2] = colors [2];
						pixels [3] = colors [3];
						pixels += rowstride;
					}
					for (; j < height; j++) {
						pixels [0] = (byte) ((j >= height - r) ? 0xff : 0x00);
						pixels [1] = (byte) ((j >= height - g) ? 0xff : 0x00);
						pixels [2] = (byte) ((j >= height - b) ? 0xff : 0x00);
						pixels [3] = 0xff;
						pixels += rowstride;
					}

				}
			}
		}	

		public Gdk.Pixbuf Generate (Gdk.Pixbuf input, int max_width)
		{
			Gdk.Pixbuf scaled;
			using (Gdk.Pixbuf pixbuf = Generate (input))
				scaled = PixbufUtils.ScaleToMaxSize (pixbuf, max_width, 128);
			return scaled;
		}

		public Gdk.Pixbuf Generate (Gdk.Pixbuf input)
		{
			FillValues (input);
			int height = 128;
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, values.GetLength (0), height);
			Draw (pixbuf);
			return pixbuf;
		}
						     
		
#if FSPOT_HISTOGRAM_MAIN
		public static void Main (string [] args) 
		{
			Gtk.Application.Init ();
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (args [0]);
			Log.DebugFormat ("loaded {0}", args [0]);
			Histogram hist = new Histogram ();
			Log.DebugFormat ("loaded histgram", args [0]);
			
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
