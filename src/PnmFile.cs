namespace FSpot.Pnm {
	public class PnmFile : ImageFile {
		public PnmFile (string path) : base (path) 
		{
			System.Console.WriteLine ("loading pnm file");
		}
		
		static string GetString (System.IO.Stream stream)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();

			char c;
			do {
				c = (char)stream.ReadByte ();
			} while (char.IsWhiteSpace (c));

			while (!char.IsWhiteSpace (c)) {
				builder.Append (c);
				c = (char)stream.ReadByte ();				
			}
			
			return builder.ToString ();
		}

		public static ushort [] ReadShort (System.IO.Stream stream, int width, int height, int channels)
		{
			int length = width * height * channels;
			ushort [] data = new ushort [length];
			byte [] tmp = new byte [2];

			for (int i = 0; i < length; i++)
			{
				stream.Read (tmp, 0, tmp.Length);
				data [i] = BitConverter.ToUInt16 (tmp, 0, false);
			}
			return data;
		}

		static Gdk.Pixbuf LoadRGB16 (System.IO.Stream stream, int width, int height)
		{
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, width, height);
			unsafe {
				byte *pixels = (byte *)pixbuf.Pixels;
				int length = width * 6;
				byte [] buffer = new byte [length];
				
				for (int row = 0; row < height; row++) {
					stream.Read (buffer, 0, buffer.Length);
					for (int i = 0; i < width * 3; i++) {
						pixels [i] = (byte) (BitConverter.ToUInt16 (buffer, i * 2, false) >> 4);
					}
					pixels += pixbuf.Rowstride;
				}
			}
			return pixbuf;
		}

		static Gdk.Pixbuf LoadRGB8 (System.IO.Stream stream, int width, int height)
		{
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, width, height);
			unsafe {
				byte *pixels = (byte *)pixbuf.Pixels;
				byte [] buffer = new byte [width * 3];
				
				for (int i = 0; i < height; i++) {
					stream.Read (buffer, 0, buffer.Length);
					System.Runtime.InteropServices.Marshal.Copy (buffer, 0, 
										     (System.IntPtr)pixels, buffer.Length);
					
					pixels += pixbuf.Rowstride; 
				}
			}
			return pixbuf;
		}

		public override Gdk.Pixbuf Load ()
		{
			try {
				System.IO.Stream stream = System.IO.File.OpenRead (this.path);
				Gdk.Pixbuf pixbuf = PnmFile.Load (stream);
				stream.Close ();
				return pixbuf;
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
			return null;
		}

		public override Gdk.Pixbuf Load (int width, int height)
		{
			return PixbufUtils.ScaleToMaxSize (this.Load (), width, height);
		}

		public static Gdk.Pixbuf Load (System.IO.Stream stream)
		{
			string magic = GetString (stream);
			int width = int.Parse (GetString (stream));
			int height = int.Parse (GetString (stream));
			ushort max = ushort.Parse (GetString (stream));
			
			System.Console.WriteLine ("Loading ({0} - {1},{2} - {3})", magic, width, height, max);

			switch (magic) {
			case "P6":
				if (max < 256)
					return LoadRGB8 (stream, width, height);
				else
					return LoadRGB16 (stream, width, height);
			default:
				throw new System.Exception (System.String.Format ("unknown pnm type {0}", magic));
			}			
		}
	}
}
