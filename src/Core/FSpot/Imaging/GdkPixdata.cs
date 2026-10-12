// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Imaging
{
	// Tag icons are stored as GdkPixdata (magic "GdkP"), which is what GTK's
	// gdk_pixdata_serialize writes. An image decoder cannot read that blob.
	public static class GdkPixdata
	{
		const uint Magic = 0x47646b50;
		const int HeaderLength = 24;
		const uint ColorRgb = 0x01;
		const uint ColorRgba = 0x02;
		const uint SampleWidth8 = 0x01 << 16;
		const uint EncodingRaw = 0x01 << 24;
		const uint EncodingRle = 0x02 << 24;
		const int MaxEdge = 2048;

		public static byte[] Write (ImageBuffer image)
		{
			int channels = Channels (image.HasAlpha);
			if (image.Rowstride < image.Width * channels)
				throw new ArgumentException ("The image row stride is shorter than one row.", nameof (image));
			int payloadLength = checked (image.Rowstride * image.Height);
			if (image.Pixels.Length < payloadLength)
				throw new ArgumentException ("The image pixel buffer is shorter than its stride.", nameof (image));

			uint type = (image.HasAlpha ? ColorRgba : ColorRgb) | SampleWidth8 | EncodingRaw;
			var stored = new byte[HeaderLength + payloadLength];
			WriteU32 (stored, 0, Magic);
			WriteU32 (stored, 4, (uint)stored.Length);
			WriteU32 (stored, 8, type);
			WriteU32 (stored, 12, (uint)image.Rowstride);
			WriteU32 (stored, 16, (uint)image.Width);
			WriteU32 (stored, 20, (uint)image.Height);
			Buffer.BlockCopy (image.Pixels, 0, stored, HeaderLength, payloadLength);
			return stored;
		}

		public static bool TryRead (byte[] data, out ImageBuffer image)
		{
			image = null;
			if (data == null || data.Length < HeaderLength || ReadU32 (data, 0) != Magic)
				return false;

			uint length = ReadU32 (data, 4);
			uint type = ReadU32 (data, 8);
			int rowstride = (int)ReadU32 (data, 12);
			int width = (int)ReadU32 (data, 16);
			int height = (int)ReadU32 (data, 20);
			uint color = type & 0xff;
			bool hasAlpha = color == ColorRgba;
			int channels = hasAlpha ? 4 : 3;
			uint encoding = type & 0xff000000;
			if (length < HeaderLength || length > data.Length)
				return false;
			if (width < 1 || height < 1 || width > MaxEdge || height > MaxEdge)
				return false;
			// Gdk aligns a row to 4 bytes, so at most three padding bytes. A wider stride is how a 24-byte icon names a multi-gigabyte buffer.
			if (rowstride < width * channels || rowstride > width * channels + 3)
				return false;
			if ((type & 0xff0000) != SampleWidth8)
				return false;
			if (color != ColorRgb && color != ColorRgba)
				return false;

			int pixelBytes;
			try {
				pixelBytes = checked (rowstride * height);
			} catch (OverflowException) {
				return false;
			}

			var payload = new ArraySegment<byte> (data, HeaderLength, (int)length - HeaderLength);
			if (encoding == EncodingRaw) {
				if (payload.Count < pixelBytes)
					return false;
			} else if (encoding != EncodingRle) {
				return false;
			}

			var pixels = new byte[pixelBytes];
			if (encoding == EncodingRaw)
				Buffer.BlockCopy (payload.Array, payload.Offset, pixels, 0, pixelBytes);
			else if (!DecodeRle (payload, channels, pixels))
				return false;

			image = new ImageBuffer (width, height, hasAlpha, rowstride, pixels);
			return true;
		}

		static bool DecodeRle (ArraySegment<byte> payload, int channels, byte[] pixels)
		{
			byte[] source = payload.Array;
			int ip = payload.Offset;
			int end = payload.Offset + payload.Count;
			int image = 0;
			while (image < pixels.Length) {
				if (ip >= end)
					return false;
				int length = source[ip++];
				if (length == 0)
					return false;
				if ((length & 128) != 0) {
					length -= 128;
					if (ip + channels > end || image + length * channels > pixels.Length)
						return false;
					for (int n = 0; n < length; n++) {
						Buffer.BlockCopy (source, ip, pixels, image, channels);
						image += channels;
					}
					ip += channels;
				} else {
					int bytes = length * channels;
					if (ip + bytes > end || image + bytes > pixels.Length)
						return false;
					Buffer.BlockCopy (source, ip, pixels, image, bytes);
					ip += bytes;
					image += bytes;
				}
			}

			return true;
		}

		static int Channels (bool hasAlpha) => hasAlpha ? 4 : 3;

		static uint ReadU32 (byte[] data, int offset)
			=> (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);

		static void WriteU32 (byte[] data, int offset, uint value)
		{
			data[offset] = (byte)(value >> 24);
			data[offset + 1] = (byte)(value >> 16);
			data[offset + 2] = (byte)(value >> 8);
			data[offset + 3] = (byte)value;
		}
	}
}
