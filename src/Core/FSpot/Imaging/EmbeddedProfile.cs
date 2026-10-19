// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Compression;

namespace FSpot.Imaging
{
	// The ICC a JPEG stores in APP2 (ICC_PROFILE) or a PNG stores in iCCP.
	// A missing, truncated, or unreadable profile is null: print then treats the
	// file as sRGB, and a bad file must not throw into a draw.
	static class EmbeddedProfile
	{
		const int MaxProfileBytes = 8 * 1024 * 1024;

		public static byte[] TryRead (byte[] data)
		{
			if (data == null || data.Length == 0)
				return null;
			return TryRead (new MemoryStream (data, writable: false));
		}

		public static byte[] TryRead (Stream stream)
		{
			if (stream == null)
				return null;

			int first = stream.ReadByte ();
			int second = stream.ReadByte ();
			if (first == 0xFF && second == 0xD8)
				return ReadJpeg (stream);
			if (first == 137 && second == (byte)'P' && LooksLikePng (stream))
				return ReadPng (stream);
			return null;
		}

		// Fail-soft. The caller owns the profile, when one is returned.
		public static Cms.Profile Open (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			try {
				using var stream = File.OpenRead (path);
				byte[] bytes = TryRead (stream);
				if (bytes == null)
					return null;
				return new Cms.Profile (bytes);
			} catch (Exception e) when (FailSoft (e)) {
				return null;
			}
		}

		static bool LooksLikePng (Stream stream)
		{
			var rest = new byte[6];
			return ReadFully (stream, rest, 6) == 6
				&& rest[0] == (byte)'N' && rest[1] == (byte)'G'
				&& rest[2] == 13 && rest[3] == 10 && rest[4] == 26 && rest[5] == 10;
		}

		static byte[] ReadJpeg (Stream stream)
		{
			var parts = new byte[256][];
			int count = 0;
			while (true) {
				int marker = NextMarker (stream);
				if (marker < 0 || marker == 0xD9 || marker == 0xDA)
					break;
				if (marker >= 0xD0 && marker <= 0xD7)
					continue;

				int length = ReadU16 (stream);
				if (length < 2)
					return null;
				int payload = length - 2;
				if (marker == 0xE2 && payload >= 14 && payload <= MaxProfileBytes) {
					var data = new byte[payload];
					if (ReadFully (stream, data, payload) != payload)
						return null;
					if (!TakeIcc (data, parts, ref count))
						return null;
				} else if (!Skip (stream, payload)) {
					return null;
				}
			}

			if (count == 0)
				return null;
			int size = 0;
			for (int i = 1; i <= count; i++) {
				if (parts[i] == null)
					return null;
				size += parts[i].Length;
			}
			if (size == 0 || size > MaxProfileBytes)
				return null;

			var profile = new byte[size];
			int at = 0;
			for (int i = 1; i <= count; i++) {
				Buffer.BlockCopy (parts[i], 0, profile, at, parts[i].Length);
				at += parts[i].Length;
			}
			return profile;
		}

		static bool TakeIcc (byte[] data, byte[][] parts, ref int count)
		{
			if (data[0] != (byte)'I' || data[1] != (byte)'C' || data[2] != (byte)'C' || data[3] != (byte)'_'
				|| data[4] != (byte)'P' || data[5] != (byte)'R' || data[6] != (byte)'O' || data[7] != (byte)'F'
				|| data[8] != (byte)'I' || data[9] != (byte)'L' || data[10] != (byte)'E' || data[11] != 0)
				return true;

			int sequence = data[12];
			int total = data[13];
			if (total < 1 || total > 255 || sequence < 1 || sequence > total)
				return false;
			if (count != 0 && count != total)
				return false;
			if (parts[sequence] != null)
				return false;

			count = total;
			var chunk = new byte[data.Length - 14];
			Buffer.BlockCopy (data, 14, chunk, 0, chunk.Length);
			parts[sequence] = chunk;
			return true;
		}

		static byte[] ReadPng (Stream stream)
		{
			var lengthBytes = new byte[4];
			var type = new byte[4];
			while (true) {
				if (ReadFully (stream, lengthBytes, 4) != 4)
					return null;
				uint length = ((uint)lengthBytes[0] << 24) | ((uint)lengthBytes[1] << 16)
					| ((uint)lengthBytes[2] << 8) | lengthBytes[3];
				if (ReadFully (stream, type, 4) != 4)
					return null;
				if (length > MaxProfileBytes)
					return null;
				var data = new byte[length];
				if (length > 0 && ReadFully (stream, data, (int)length) != (int)length)
					return null;
				if (!Skip (stream, 4))
					return null;
				if (type[0] == (byte)'I' && type[1] == (byte)'E' && type[2] == (byte)'N' && type[3] == (byte)'D')
					return null;
				if (type[0] == (byte)'i' && type[1] == (byte)'C' && type[2] == (byte)'C' && type[3] == (byte)'P')
					return InflateIccp (data);
			}
		}

		static byte[] InflateIccp (byte[] data)
		{
			int keywordEnd = 0;
			while (keywordEnd < data.Length && data[keywordEnd] != 0)
				keywordEnd++;
			if (keywordEnd == 0 || keywordEnd > 79 || keywordEnd + 2 > data.Length)
				return null;
			if (data[keywordEnd + 1] != 0)
				return null;
			return InflateZlib (data, keywordEnd + 2, data.Length - keywordEnd - 2);
		}

		static byte[] InflateZlib (byte[] data, int offset, int length)
		{
			if (length < 6)
				return null;
			int cmf = data[offset];
			int flg = data[offset + 1];
			if ((cmf & 0x0F) != 8)
				return null;
			if (((cmf << 8) + flg) % 31 != 0)
				return null;
			if ((flg & 0x20) != 0)
				return null;

			int deflateLength = length - 6;
			try {
				using var input = new MemoryStream (data, offset + 2, deflateLength, writable: false);
				using var deflate = new DeflateStream (input, CompressionMode.Decompress);
				using var output = new MemoryStream ();
				var buffer = new byte[8192];
				int total = 0;
				while (true) {
					int n = deflate.Read (buffer, 0, buffer.Length);
					if (n == 0)
						break;
					total += n;
					if (total > MaxProfileBytes)
						return null;
					output.Write (buffer, 0, n);
				}
				return output.Length == 0 ? null : output.ToArray ();
			} catch (InvalidDataException) {
				return null;
			}
		}

		static int NextMarker (Stream stream)
		{
			int b = stream.ReadByte ();
			if (b != 0xFF)
				return -1;
			do {
				b = stream.ReadByte ();
			} while (b == 0xFF);
			return b;
		}

		static int ReadU16 (Stream stream)
		{
			int hi = stream.ReadByte ();
			int lo = stream.ReadByte ();
			if (hi < 0 || lo < 0)
				return -1;
			return (hi << 8) | lo;
		}

		static int ReadFully (Stream stream, byte[] buffer, int count)
		{
			int read = 0;
			while (read < count) {
				int n = stream.Read (buffer, read, count - read);
				if (n <= 0)
					break;
				read += n;
			}
			return read;
		}

		static bool Skip (Stream stream, int count)
		{
			var buffer = new byte[8192];
			while (count > 0) {
				int n = stream.Read (buffer, 0, Math.Min (buffer.Length, count));
				if (n <= 0)
					return false;
				count -= n;
			}
			return true;
		}

		static bool FailSoft (Exception e)
			=> e is Cms.CmsException || e is IOException || e is UnauthorizedAccessException
				|| Cms.ProfileCatalog.IsNativeLibraryMissing (e);
	}
}
