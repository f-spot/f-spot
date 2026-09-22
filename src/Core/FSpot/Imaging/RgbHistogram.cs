// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Imaging
{
	public static class RgbHistogram
	{
		public const int Red = 0;
		public const int Green = 1;
		public const int Blue = 2;

		public static int[,] CountRgb (ImageBuffer buffer)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));

			var values = new int[256, 3];
			int channels = buffer.HasAlpha ? 4 : 3;
			byte[] pixels = buffer.Pixels;
			for (int y = 0; y < buffer.Height; y++) {
				int row = y * buffer.Rowstride;
				for (int x = 0; x < buffer.Width; x++) {
					int i = row + x * channels;
					values[pixels[i], Red]++;
					values[pixels[i + 1], Green]++;
					values[pixels[i + 2], Blue]++;
				}
			}

			return values;
		}

		public static void GetHighLow (int[,] values, int channel, out int high, out int low)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));

			double total = 0;
			int bins = values.GetLength (0);
			for (int i = 0; i < bins; i++)
				total += values[i, channel];

			double current = 0.0;
			low = 0;
			high = 0;
			if (total <= 0)
				return;

			for (int i = 0; i < bins - 1; i++) {
				current += values[i, channel];
				double percentage = current / total;
				double next = (current + values[i + 1, channel]) / total;
				if (Math.Abs (percentage - 0.006) < Math.Abs (next - 0.006)) {
					low = i + 1;
					break;
				}
			}

			for (int i = bins - 1; i > 0; i--) {
				current += values[i, channel];
				double percentage = current / total;
				double next = (current + values[i - 1, channel]) / total;
				if (Math.Abs (percentage - 0.006) < Math.Abs (next - 0.006)) {
					high = i - 1;
					break;
				}
			}
		}

		public static int[,] CountBgra (byte[] bgra)
		{
			if (bgra == null)
				throw new ArgumentNullException (nameof (bgra));

			var values = new int[256, 3];
			int last = bgra.Length - 3;
			for (int i = 0; i < last; i += 4) {
				values[bgra[i + 2], Red]++;
				values[bgra[i + 1], Green]++;
				values[bgra[i], Blue]++;
			}

			return values;
		}

		public static byte[] RenderBgra (int[,] values, int height)
			=> RenderBgra (values, 256, height);

		public static byte[] RenderBgra (int[,] values, int width, int height)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));
			if (height <= 0)
				throw new ArgumentOutOfRangeException (nameof (height));
			if (width <= 0)
				throw new ArgumentOutOfRangeException (nameof (width));

			int max = 1;
			int bins = Math.Min (256, values.GetLength (0));
			for (int i = 0; i < bins; i++) {
				max = Math.Max (max, values[i, Red]);
				max = Math.Max (max, values[i, Green]);
				max = Math.Max (max, values[i, Blue]);
			}

			var pixels = new byte[width * height * 4];
			for (int x = 0; x < width; x++) {
				int bin = Math.Min (bins - 1, x * bins / width);
				int r = values[bin, Red] * height / max;
				int g = values[bin, Green] * height / max;
				int b = values[bin, Blue] * height / max;
				int top = Math.Max (r, Math.Max (g, b));
				for (int y = 0; y < height; y++) {
					int fromBottom = height - 1 - y;
					int i = (y * width + x) * 4;
					if (fromBottom >= top) {
						pixels[i] = 0;
						pixels[i + 1] = 0;
						pixels[i + 2] = 0;
						pixels[i + 3] = 255;
					} else {
						pixels[i] = (byte)(fromBottom < b ? 255 : 0);
						pixels[i + 1] = (byte)(fromBottom < g ? 255 : 0);
						pixels[i + 2] = (byte)(fromBottom < r ? 255 : 0);
						pixels[i + 3] = 255;
					}
				}
			}

			return pixels;
		}
	}
}
