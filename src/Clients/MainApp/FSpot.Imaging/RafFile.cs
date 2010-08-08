using FSpot.Utils;
using Hyena;
using TagLib.Image;

namespace FSpot.Imaging {
	// This is reverse engineered from looking at the sample files I have
	// from what I can tell the file is always BigEndian, although the embedded jpeg may not be
	// and there is a start long offset at 0x54 (or possibly 0x56 if it is a short) that points to
	// the start of the embedded jpeg and followed by a long length that gives the length of the jpeg
	// data.
	//
	// Following that there seem to be more offsets and lengths (probably for the raw data) that I haven't
	// completely figured out yet.  More to follow.

	// ALL the sample files I have begin with "FUJIFILMCCD-RAW "

	public class RafFile : BaseImageFile {

		public RafFile (SafeUri uri) : base (uri)
		{
		}

		public override System.IO.Stream PixbufStream ()
		{
			byte [] data = GetEmbeddedJpeg ();

			if (data != null)
				return new System.IO.MemoryStream (data);
			else
				return DCRawFile.RawPixbufStream (Uri);
		}

		private byte [] GetEmbeddedJpeg ()
		{
			using (System.IO.Stream stream = base.PixbufStream ()) {
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
