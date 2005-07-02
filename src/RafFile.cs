namespace FSpot.Raf {
	// This is reverse engineered from looking at the sample files I have.
	// from what I can tell the file is always BigEndian, althogh the embedded jpeg may not be
	// and there is a start long offset at 0x54 (or possibly 0x56 if it is a short) that points to
	// the start of the embedded jpeg and followed by a long length that gives the length of the jpeg
	// data.   
	//
	// Following that there seem to be more offsets and lengths (probably for the raw data) that I haven't
	// completely figured out yet.  More to follow.

	public class RafFile : ImageFile {
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

		private byte [] GetEmbeddedJpeg ()
		{
			using (System.IO.Stream stream = System.IO.File.OpenRead (this.path)) {
				stream.Position = 0x56;
				byte [] data = new byte [8];
				stream.Read (data, 0, data.Length);
				uint offset = BitConverter.ToUInt32 (data, 0, false);
				uint length = BitConverter.ToUInt32 (data, 4, false);
				
				byte [] image = new byte [length];
				stream.Position = offset;
				stream.Read (image, 0, image.Length);
				return image;
			}

		}
	}
}
