//
// StreamExtensions.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace FSpot.Utils
{
	public static class StreamExtensions
	{
		// Stream.Read may return fewer bytes than asked for, and callers that parse the
		// buffer afterwards -- the CIFF and RAF headers, for instance -- read offsets and
		// lengths out of whatever it left behind. This is .NET 7's Stream.ReadExactly,
		// which FSpot.dll cannot use while it targets net472; when it moves to net10 this
		// whole file is deletable and the call sites keep working.
		public static void ReadExactly (this Stream stream, byte[] buffer, int offset, int count)
		{
			if (stream == null)
				throw new ArgumentNullException (nameof (stream));
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			// Split the way Stream.ValidateBufferArguments splits it, so the exception
			// names the argument that is wrong. One combined check reported "count" even
			// when offset was the problem.
			if (offset < 0)
				throw new ArgumentOutOfRangeException (nameof (offset));
			if (count < 0 || buffer.Length - offset < count)
				throw new ArgumentOutOfRangeException (nameof (count));

			int read = 0;
			while (read < count) {
				int got = stream.Read (buffer, offset + read, count - read);
				if (got == 0)
					throw new EndOfStreamException ($"Expected {count} bytes but the stream ended after {read}");

				read += got;
			}
		}
	}
}
