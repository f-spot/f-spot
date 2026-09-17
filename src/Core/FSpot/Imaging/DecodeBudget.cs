// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace FSpot.Imaging
{
	public static class DecodeBudget
	{
		// 192 megapixels is about 800 MB of RGBA. A decoder refuses a header
		// past this before it allocates the bitmap.
		public const long MaxPixels = 192L * 1000 * 1000;

		public const string TooLargeMessage = "The image is too large to decode.";

		// Held for one decode so a test can prove a decoder reads this limit.
		static long limitedTo;
		static readonly object gate = new object ();

		public static long ActiveMaxPixels {
			get {
				lock (gate)
					return limitedTo > 0 ? limitedTo : MaxPixels;
			}
		}

		public static bool Exceeds (int width, int height)
			=> width <= 0 || height <= 0 || (long)width * height > ActiveMaxPixels;

		public static void ThrowIfExceeds (int width, int height)
		{
			if (Exceeds (width, height))
				throw new InvalidDataException (TooLargeMessage);
		}

		internal static IDisposable LimitForTest (long pixels)
		{
			Monitor.Enter (gate);
			limitedTo = pixels;
			return new RestoreLimit ();
		}

		sealed class RestoreLimit : IDisposable
		{
			public void Dispose ()
			{
				limitedTo = 0;
				Monitor.Exit (gate);
			}
		}
	}
}
