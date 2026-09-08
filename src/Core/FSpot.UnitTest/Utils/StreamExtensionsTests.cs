//
// StreamExtensionsTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.UnitTest.Utils
{
	// Called statically throughout: net10 has its own Stream.ReadExactly, so the
	// extension-method form would bind to the BCL here and test nothing of ours.
	[TestClass]
	public class StreamExtensionsTests
	{
		// Stream.Read is allowed to return fewer bytes than asked for, and the RAW
		// parsers were treating a short read as a full one -- parsing offsets and
		// lengths out of a partly-filled buffer. This stream reproduces that by handing
		// back one byte at a time.
		sealed class DripStream : Stream
		{
			readonly byte[] data;
			int position;

			public DripStream (byte[] data) => this.data = data;

			public override int Read (byte[] buffer, int offset, int count)
			{
				if (position >= data.Length || count == 0)
					return 0;

				buffer[offset] = data[position++];

				return 1;
			}

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => data.Length;
			public override long Position { get => position; set => throw new NotSupportedException (); }
			public override void Flush () { }
			public override long Seek (long offset, SeekOrigin origin) => throw new NotSupportedException ();
			public override void SetLength (long value) => throw new NotSupportedException ();
			public override void Write (byte[] buffer, int offset, int count) => throw new NotSupportedException ();
		}

		[TestMethod]
		public void ReadExactly_FillsTheBufferWhenTheStreamReturnsOneByteAtATime ()
		{
			var stream = new DripStream (new byte[] { 1, 2, 3, 4, 5 });
			var buffer = new byte[5];

			StreamExtensions.ReadExactly (stream, buffer, 0, buffer.Length);

			Assert.AreSequenceEqual (new byte[] { 1, 2, 3, 4, 5 }, buffer);
		}

		[TestMethod]
		public void ReadExactly_HonoursTheOffsetAndLeavesTheRestOfTheBufferAlone ()
		{
			var stream = new DripStream (new byte[] { 7, 8 });
			var buffer = new byte[4];

			StreamExtensions.ReadExactly (stream, buffer, 1, 2);

			Assert.AreSequenceEqual (new byte[] { 0, 7, 8, 0 }, buffer);
		}

		// A truncated file has to be an error, not a buffer of zeroes parsed as offsets.
		[TestMethod]
		public void ReadExactly_ThrowsEndOfStreamWhenThereIsNotEnoughData ()
		{
			var stream = new DripStream (new byte[] { 1, 2 });

			Assert.ThrowsExactly<EndOfStreamException> (() => StreamExtensions.ReadExactly (stream, new byte[4], 0, 4));
		}

		[TestMethod]
		public void ReadExactly_ReadingNothingIsAllowed ()
		{
			var stream = new DripStream (Array.Empty<byte> ());

			StreamExtensions.ReadExactly (stream, Array.Empty<byte> (), 0, 0);
		}

		// The exception has to name the argument that is wrong, the way
		// Stream.ValidateBufferArguments does: one combined range check reported "count"
		// even for a negative offset, which sends the reader to the wrong argument.
		[TestMethod]
		[DynamicData (nameof (BadBufferArguments))]
		public void ReadExactly_NamesTheArgumentThatIsOutOfRange (int offset, int count, string expected)
		{
			var stream = new DripStream (new byte[] { 1, 2, 3, 4 });

			var thrown = Assert.ThrowsExactly<ArgumentOutOfRangeException> (
				() => StreamExtensions.ReadExactly (stream, new byte[4], offset, count));

			Assert.AreEqual (expected, thrown.ParamName);
		}

		public static IEnumerable<(int offset, int count, string expected)> BadBufferArguments =>
		[
			(-1, 1, "offset"),
			(0, -1, "count"),
			(3, 2, "count"),
			(-1, -1, "offset"),
		];
	}
}
