// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

using FSpot.Cms;
using FSpot.Cms.Tests;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging
{
	[TestClass]
	public sealed class EmbeddedProfileTests
	{
		[TestMethod]
		public void Jpeg_ReassemblesApp2SegmentsInOrder ()
		{
			byte[] profile = MinimalIccProfile.WithColorSpace ("RGB ");
			int split = profile.Length / 2;
			byte[] first = profile[..split];
			byte[] second = profile[split..];
			byte[] jpeg = Jpeg (new (byte Sequence, byte Count, byte[] Data)[] {
				(2, 2, second),
				(1, 2, first),
			});

			byte[] read = EmbeddedProfile.TryRead (jpeg);

			Assert.AreSequenceEqual (profile, read);
		}

		[TestMethod]
		public void Jpeg_MissingASegmentReturnsNull ()
		{
			byte[] profile = MinimalIccProfile.WithColorSpace ("RGB ");
			byte[] jpeg = Jpeg (new (byte, byte, byte[])[] { (1, 2, profile) });

			Assert.IsNull (EmbeddedProfile.TryRead (jpeg));
		}

		[TestMethod]
		public void Png_InflatesTheIccProfile ()
		{
			byte[] profile = MinimalIccProfile.WithColorSpace ("RGB ");
			byte[] png = Png (profile);

			byte[] read = EmbeddedProfile.TryRead (png);

			Assert.AreSequenceEqual (profile, read);
		}

		[TestMethod]
		public void UnrecognizedBytes_ReturnNull ()
		{
			Assert.IsNull (EmbeddedProfile.TryRead (new byte[] { 1, 2, 3, 4 }));
			Assert.IsNull (EmbeddedProfile.TryRead (Array.Empty<byte> ()));
			Assert.IsNull (EmbeddedProfile.TryRead ((byte[])null));
		}

		[TestMethod]
		public void GetProfile_ReturnsTheEmbeddedProfile ()
		{
			LcmsNative.Require ();
			string path = Path.Combine (Path.GetTempPath (), "fspot-icc-" + Guid.NewGuid ().ToString ("N") + ".jpg");
			try {
				File.WriteAllBytes (path, Jpeg (new (byte, byte, byte[])[] {
					(1, 1, MinimalIccProfile.WithColorSpace ("RGB ")),
				}));

				using var image = new BaseImageFile (new SafeUri (path));
				using Profile profile = image.GetProfile ();

				Assert.IsNotNull (profile);
				Assert.AreEqual (IccColorSpace.Rgb, profile.ColorSpace);
			} finally {
				if (File.Exists (path))
					File.Delete (path);
			}
		}

		[TestMethod]
		public void GetProfile_IsNullWhenTheJpegHasNoProfile ()
		{
			string path = Path.Combine (Path.GetTempPath (), "fspot-icc-" + Guid.NewGuid ().ToString ("N") + ".jpg");
			try {
				File.WriteAllBytes (path, new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

				using var image = new BaseImageFile (new SafeUri (path));

				Assert.IsNull (image.GetProfile ());
			} finally {
				if (File.Exists (path))
					File.Delete (path);
			}
		}

		[TestMethod]
		public void GetProfile_IsNullWhenTheEmbeddedBytesAreNotAProfile ()
		{
			LcmsNative.Require ();
			string path = Path.Combine (Path.GetTempPath (), "fspot-icc-" + Guid.NewGuid ().ToString ("N") + ".jpg");
			try {
				File.WriteAllBytes (path, Jpeg (new (byte, byte, byte[])[] {
					(1, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }),
				}));

				using var image = new BaseImageFile (new SafeUri (path));

				Assert.IsNull (image.GetProfile ());
			} finally {
				if (File.Exists (path))
					File.Delete (path);
			}
		}

		static byte[] Jpeg ((byte Sequence, byte Count, byte[] Data)[] segments)
		{
			using var stream = new MemoryStream ();
			stream.WriteByte (0xFF);
			stream.WriteByte (0xD8);
			foreach (var segment in segments) {
				var payload = new byte[14 + segment.Data.Length];
				Encoding.ASCII.GetBytes ("ICC_PROFILE").CopyTo (payload, 0);
				payload[12] = segment.Sequence;
				payload[13] = segment.Count;
				segment.Data.CopyTo (payload, 14);
				stream.WriteByte (0xFF);
				stream.WriteByte (0xE2);
				int length = payload.Length + 2;
				stream.WriteByte ((byte)(length >> 8));
				stream.WriteByte ((byte)length);
				stream.Write (payload, 0, payload.Length);
			}
			stream.WriteByte (0xFF);
			stream.WriteByte (0xD9);
			return stream.ToArray ();
		}

		static byte[] Png (byte[] profile)
		{
			byte[] compressed = Zlib (profile);
			var chunk = new byte[3 + 1 + 1 + compressed.Length];
			chunk[0] = (byte)'i';
			chunk[1] = (byte)'c';
			chunk[2] = (byte)'c';
			chunk[3] = 0;
			chunk[4] = 0;
			compressed.CopyTo (chunk, 5);

			using var stream = new MemoryStream ();
			stream.Write (new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
			WriteChunk (stream, "iCCP", chunk);
			WriteChunk (stream, "IEND", Array.Empty<byte> ());
			return stream.ToArray ();
		}

		static void WriteChunk (Stream stream, string type, byte[] data)
		{
			WriteU32 (stream, (uint)data.Length);
			byte[] name = Encoding.ASCII.GetBytes (type);
			stream.Write (name, 0, name.Length);
			stream.Write (data, 0, data.Length);
			stream.Write (new byte[4], 0, 4);
		}

		static void WriteU32 (Stream stream, uint value)
		{
			stream.WriteByte ((byte)(value >> 24));
			stream.WriteByte ((byte)(value >> 16));
			stream.WriteByte ((byte)(value >> 8));
			stream.WriteByte ((byte)value);
		}

		static byte[] Zlib (byte[] data)
		{
			using var stream = new MemoryStream ();
			using (var zlib = new ZLibStream (stream, CompressionLevel.SmallestSize, leaveOpen: true))
				zlib.Write (data, 0, data.Length);
			return stream.ToArray ();
		}
	}
}
