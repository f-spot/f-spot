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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Hyena;

namespace FSpot.Imaging
{
	// This is reverse engineered from looking at the sample files I have
	// from what I can tell the file is always BigEndian, although the embedded jpeg may not be
	// and there is a start long offset at 0x54 (or possibly 0x56 if it is a short) that points to
	// the start of the embedded jpeg and followed by a long length that gives the length of the jpeg
	// data.
	//
	// Following that there seem to be more offsets and lengths (probably for the raw data) that I haven't
	// completely figured out yet.  More to follow.

	// ALL the sample files I have begin with "FUJIFILMCCD-RAW "

	class RafImageFile : BaseImageFile
	{

		public RafImageFile (SafeUri uri) : base (uri)
		{
		}

		public override Stream PixbufStream ()
		{
			byte[] data = GetEmbeddedJpeg ();

			return data != null ? new MemoryStream (data) : DCRawImageFile.RawPixbufStream (Uri);
		}

		byte[] GetEmbeddedJpeg ()
		{
			using Stream stream = base.PixbufStream ();
			stream.Position = 0x54;
			var data = new byte[24];
			stream.Read (data, 0, data.Length);
			uint jpeg_offset = BitConverter.ToUInt32 (data, 0, false);
			uint jpeg_length = BitConverter.ToUInt32 (data, 4, false);

			// FIXME implement wb parsing
			//uint wb_offset = BitConverter.ToUInt32 (data, 8, false);
			//uint wb_length = BitConverter.ToUInt32 (data, 12, false);

			// FIXME implement decoding
			//uint raw_offset = BitConverter.ToUInt32 (data, 16, false);
			//uint raw_length = BitConverter.ToUInt32 (data, 20, false);

			var image = new byte[jpeg_length];
			stream.Position = jpeg_offset;
			stream.Read (image, 0, image.Length);
			return image;
		}
	}
}
