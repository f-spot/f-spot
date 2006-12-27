/* 
 *  
 */

using System;

namespace FSpot {
	public class Histogram {
		public Histogram (Gdk.Pixbuf src)
		{
		        FillValues (src);
		}
		
		public Histogram () {}
		
		// FIXME these should be properties
		public byte [] Color = new byte [] {0x00, 0x00, 0x00, 0xff};

		public void FillValues (Gdk.Pixbuf src)

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
			if (pixbuf != null) {
				Draw (pixbuf);
			}
		}
		
		public int Count (int channel)
		{
			int count = 0;
			for (int i = 0; i < values.GetLength (0); i++) {
				count += values [i, channel];
			}

			return count;
		}

		public void GetHighLow (int channel, out int high, out int low)
		{
			double total = Count (channel);
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
						pixels [0] = Color [0];
						pixels [1] = Color [1];
						pixels [2] = Color [2];
						pixels [3] = Color [3];
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

		public Gdk.Pixbuf GeneratePixbuf ()
		{
			int height = 128;
			pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, values.GetLength (0), height);
			this.Draw (pixbuf);
			return pixbuf;
		}
						     
		private int [,] values = new int [256, 3];
		public int [,] Values {
			get {
				return values;
			}
		}
		
		private Gdk.Pixbuf pixbuf;
		public Gdk.Pixbuf Pixbuf {
			get {
				return pixbuf;
			}
		}
		
#if FSPOT_HISTOGRAM_MAIN
		public static void Main (string [] args) 
		{
			Gtk.Application.Init ();
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (args [0]);
			System.Console.WriteLine ("loaded {0}", args [0]);
			Histogram hist = new Histogram (pixbuf);
			System.Console.WriteLine ("loaded histgram", args [0]);
			
			Gtk.Window win = new Gtk.Window ("display");
			Gtk.Image image = new Gtk.Image ();
			Gdk.Pixbuf img = hist.GeneratePixbuf ();
			image.Pixbuf = img;
			win.Add (image);
			win.ShowAll ();
			Gtk.Application.Run ();

		}
#endif
	}
}
