//
// RafImageFile.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using Hyena;

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

	class RafImageFile : BaseImageFile {

		public RafImageFile (SafeUri uri) : base (uri)
		{
		}

		public override Stream PixbufStream ()
		{
			byte [] data = GetEmbeddedJpeg ();

			return data != null ? new MemoryStream (data) : DCRawImageFile.RawPixbufStream (Uri);
		}

		byte [] GetEmbeddedJpeg ()
		{
			using (Stream stream = base.PixbufStream ()) {
				stream.Position = 0x54;
				var data = new byte [24];
				stream.Read (data, 0, data.Length);
				uint jpeg_offset = BitConverter.ToUInt32 (data, 0, false);
				uint jpeg_length = BitConverter.ToUInt32 (data, 4, false);

				// FIXME implement wb parsing
				//uint wb_offset = BitConverter.ToUInt32 (data, 8, false);
				//uint wb_length = BitConverter.ToUInt32 (data, 12, false);

				// FIXME implement decoding
				//uint raw_offset = BitConverter.ToUInt32 (data, 16, false);
				//uint raw_length = BitConverter.ToUInt32 (data, 20, false);

				var image = new byte [jpeg_length];
				stream.Position = jpeg_offset;
				stream.Read (image, 0, image.Length);
				return image;
			}
		}
	}
}
