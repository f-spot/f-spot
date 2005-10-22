namespace FSpot.Raf {
	// This is reverse engineered from looking at the sample files I have
	// from what I can tell the file is always BigEndian, although the embedded jpeg may not be
	// and there is a start long offset at 0x54 (or possibly 0x56 if it is a short) that points to
	// the start of the embedded jpeg and followed by a long length that gives the length of the jpeg
	// data.   
	//
	// Following that there seem to be more offsets and lengths (probably for the raw data) that I haven't
	// completely figured out yet.  More to follow.

	// ALL the sample files I have begin with "FUJIFILMCCD-RAW "

	
	public class WhiteBalance {
		// see dcraw parse_fuli
		public WhiteBalance (System.IO.Stream stream)
		{

		}
	}
	
	public class RafFile : ImageFile, SemWeb.StatementSource {
		public RafFile (string path) : base (path)
		{
			
		}

		public override System.IO.Stream PixbufStream ()
		{
			System.IO.MemoryStream stream = null; 
			byte [] data = GetEmbeddedJpeg ();
			
			if (data != null)
				stream = new System.IO.MemoryStream (data);
				
			return stream;
		}
 
		public override Gdk.Pixbuf Load ()
		{
			// FIXME this is a hack. No, really, I mean it.
			
			byte [] data = GetEmbeddedJpeg ();
			if (data != null) {
				Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();
				loader.Write (data, (ulong)data.Length);
				Gdk.Pixbuf pixbuf = loader.Pixbuf;
				loader.Close ();
				return pixbuf;
			}
			return null;
		}

		public override Gdk.Pixbuf Load (int width, int height)
		{
			Gdk.Pixbuf full = this.Load ();
			Gdk.Pixbuf scaled  = PixbufUtils.ScaleToMaxSize (full, width, height);
			full.Dispose ();
			return scaled;
		}

		public void Select (SemWeb.StatementSink sink)
		{
			byte [] data = GetEmbeddedJpeg ();
			if (data != null) {
				System.IO.Stream stream = new System.IO.MemoryStream (data);
				JpegHeader header = new JpegHeader (stream);
				header.Select (sink);
			}
		}

		private byte [] GetEmbeddedJpeg ()
		{
			using (System.IO.Stream stream = System.IO.File.OpenRead (this.path)) {
				stream.Position = 0x54;
				byte [] data = new byte [24];
				stream.Read (data, 0, data.Length);
				uint jpeg_offset = BitConverter.ToUInt32 (data, 0, false);
				uint jpeg_length = BitConverter.ToUInt32 (data, 4, false);

				// FIXME implement wb parsing
				//uint wb_offset = BitConverter.ToUInt32 (data, 8, false);
				//uint wb_length = BitConverter.ToUInt32 (data, 12, false);
				
				// FIXME implement decoding
				//uint raw_offset = BitConverter.ToUInt32 (data, 16, false);
				//uint raw_length = BitConverter.ToUInt32 (data, 20, false);

				byte [] image = new byte [jpeg_length];
				stream.Position = jpeg_offset;
				stream.Read (image, 0, image.Length);
				return image;
			}

		}
	}
}
