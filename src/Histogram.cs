namespace FSpot {
	public class Histogram {
		private enum Channel 
		{
			Red = 0,
			Green = 1,
			Blue = 2,
			Alpha = 3
		}
		
		public bool UseAlpha = true;

		public Histogram (Gdk.Pixbuf src)
		{
		        FillValues (src);
		}
		
		public Histogram () {}
		
		public void FillValues (Gdk.Pixbuf src)

		{
			values = new int [256, 3];

			if (src.BitsPerSample != 8)
				throw new System.Exception ("Invalid bits per sample");
						
			unsafe {
				byte * srcb = (byte *)src.Pixels;
				for (int j = 0; j < src.Height; j++) {
					for (int i = 0; i < src.Width; i++) {
						values [*(srcb++), (int)Channel.Red]++;
						values [*(srcb++), (int)Channel.Green]++;
						values [*(srcb++), (int)Channel.Blue]++;
						if (src.HasAlpha)
							srcb++;
					}
					srcb =  ((byte *) src.Pixels) + j * src.Rowstride;
				}
			}
			if (pixbuf != null) {
				FillPixbuf (pixbuf);
			}
		}
		
		private void FillPixbuf (Gdk.Pixbuf image) 
		{
			int max = 0;
			for (int i = 0; i < values.GetLength (0); i++) {
				for (int j = 0; j < values.GetLength (1); j++) {
					max = System.Math.Max (max, values [i, j]);
				}
			}
			unsafe {
				byte * pixels = (byte *)image.Pixels;
				pixels += (image.Height -1) * image.Rowstride;

				for (int j = 0; j < image.Height; j++) {
					for (int i = 0; i < image.Width; i++) {
						byte found = 0x00;
						byte  *offset = pixels + i * image.NChannels;

						*(offset++)   = (j < image.Height * (values [i, (int)Channel.Red]/(double)max)) ? found = (byte)0xff : (byte)0x00;
						*(offset++) = (j < image.Height * (values [i, (int)Channel.Green]/(double)max)) ? found = (byte)0xff : (byte)0x00;
						*(offset++)  = (j < image.Height * (values [i, (int)Channel.Blue]/(double)max)) ? found = (byte)0xff : (byte)0x00;

						if (UseAlpha)
							*(offset++) = found;
						else 
							*(offset++) = 0xff;
					}
					pixels -= image.Rowstride;
				}
			}
		}	

		public Gdk.Pixbuf GeneratePixbuf ()
		{
			int height = 128;
			pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, values.GetLength (0), height);
			this.FillPixbuf (pixbuf);
			return pixbuf;
		}
						     
		private int [,] values = new int [256,3];
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
