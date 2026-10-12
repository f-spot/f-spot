// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public sealed class GdkPixdataTests
	{
		[TestMethod]
		public void WriteThenRead_KeepsRawRgbPixels ()
		{
			var original = new ImageBuffer (2, 1, false, 6, new byte[] { 1, 2, 3, 4, 5, 6 });

			byte[] stored = GdkPixdata.Write (original);

			Assert.IsTrue (GdkPixdata.TryRead (stored, out ImageBuffer loaded));
			Assert.AreEqual (original.Width, loaded.Width);
			Assert.AreEqual (original.Height, loaded.Height);
			Assert.IsFalse (loaded.HasAlpha);
			Assert.AreSequenceEqual (original.Pixels, loaded.Pixels);
		}

		[TestMethod]
		public void WriteThenRead_KeepsRawRgbaPixels ()
		{
			var original = new ImageBuffer (2, 1, true, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

			Assert.IsTrue (GdkPixdata.TryRead (GdkPixdata.Write (original), out ImageBuffer loaded));

			Assert.IsTrue (loaded.HasAlpha);
			Assert.AreSequenceEqual (original.Pixels, loaded.Pixels);
		}

		[TestMethod]
		public void TryRead_DecodesAnRleRun ()
		{
			// 2x1 RGBA, one run of two identical pixels. Type is RGBA | 8-bit | RLE.
			byte[] stored = Header (width: 2, height: 1, rowstride: 8, type: 0x02010002, payload: new byte[] {
				0x82, 9, 8, 7, 6
			});

			Assert.IsTrue (GdkPixdata.TryRead (stored, out ImageBuffer loaded));

			Assert.AreSequenceEqual (new byte[] { 9, 8, 7, 6, 9, 8, 7, 6 }, loaded.Pixels);
		}

		[TestMethod]
		public void TryRead_RejectsAPngSoTheCallerCanUseAnImageDecoder ()
		{
			Assert.IsFalse (GdkPixdata.TryRead (new byte[] { 0x89, 0x50, 0x4e, 0x47 }, out _));
		}

		[TestMethod]
		public void TryRead_RejectsATruncatedHeader ()
		{
			Assert.IsFalse (GdkPixdata.TryRead (new byte[] { 0x47, 0x64, 0x6b, 0x50 }, out _));
		}

		[TestMethod]
		public void TryRead_RejectsARowstrideWiderThanTheGdkPad ()
		{
			// Gdk aligns a row to 4 bytes, so a stored stride is at most width * channels + 3.
			const int width = 1;
			const int height = 1;
			const int channels = 3;
			int rowstride = width * channels + 4;
			byte[] stored = Header (width, height, rowstride, type: 0x01010001, payload: new byte[rowstride]);

			Assert.IsFalse (GdkPixdata.TryRead (stored, out ImageBuffer loaded));
			Assert.IsNull (loaded);
		}

		[TestMethod]
		public void TryRead_RejectsAShortRawPayloadBeforeAllocatingThePixels ()
		{
			// A legal stride can still name a buffer the payload does not contain.
			const int width = 512;
			const int height = 512;
			int rowstride = width * 3 + 3;
			byte[] stored = Header (width, height, rowstride, type: 0x01010001, payload: new byte[] { 0 });

			long before = GC.GetAllocatedBytesForCurrentThread ();
			bool read = GdkPixdata.TryRead (stored, out ImageBuffer loaded);
			long allocated = GC.GetAllocatedBytesForCurrentThread () - before;

			Assert.IsFalse (read);
			Assert.IsNull (loaded);
			Assert.IsLessThan (64 * 1024, allocated);
		}

		[TestMethod]
		public void TryRead_KeepsARowstrideAtTheGdkPad ()
		{
			const int width = 1;
			const int height = 1;
			int rowstride = width * 3 + 3;
			var payload = new byte[rowstride];
			payload[0] = 9;
			payload[1] = 8;
			payload[2] = 7;
			byte[] stored = Header (width, height, rowstride, type: 0x01010001, payload: payload);

			Assert.IsTrue (GdkPixdata.TryRead (stored, out ImageBuffer loaded));

			Assert.AreEqual (rowstride, loaded.Rowstride);
			Assert.AreEqual (9, loaded.Pixels[0]);
			Assert.AreEqual (8, loaded.Pixels[1]);
			Assert.AreEqual (7, loaded.Pixels[2]);
		}

		static byte[] Header (int width, int height, int rowstride, uint type, byte[] payload)
		{
			int length = 24 + payload.Length;
			var stored = new byte[length];
			WriteU32 (stored, 0, 0x47646b50);
			WriteU32 (stored, 4, (uint)length);
			WriteU32 (stored, 8, type);
			WriteU32 (stored, 12, (uint)rowstride);
			WriteU32 (stored, 16, (uint)width);
			WriteU32 (stored, 20, (uint)height);
			payload.CopyTo (stored, 24);
			return stored;
		}

		static void WriteU32 (byte[] data, int offset, uint value)
		{
			data[offset] = (byte)(value >> 24);
			data[offset + 1] = (byte)(value >> 16);
			data[offset + 2] = (byte)(value >> 8);
			data[offset + 3] = (byte)value;
		}
	}
}
