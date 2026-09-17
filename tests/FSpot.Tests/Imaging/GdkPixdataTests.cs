// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
