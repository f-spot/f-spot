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

			int bins = values.GetLength (0);
			double total = 0;
			for (int i = 0; i < bins; i++)
				total += values[i, channel];

			low = 0;
			high = 0;
			if (total <= 0 || bins < 2)
				return;

			low = Cut (values, channel, bins, total, fromStart: true);
			high = Cut (values, channel, bins, total, fromStart: false);
		}

		// Each end starts at zero. An empty run is not a step: before any pixels are clipped the
		// cut is the next occupied code, and after that it stays at the end of the clipped prefix.
		const double ClipFraction = 0.006;

		static int Cut (int[,] values, int channel, int bins, double total, bool fromStart)
		{
			double current = 0;
			if (fromStart) {
				for (int i = 0; i < bins - 1; i++) {
					current += values[i, channel];
					int nextBin = NextOccupied (values, channel, i + 1, bins - 1, +1);
					if (CloserThanNext (current, values[nextBin, channel], total))
						return current == 0 ? nextBin : i + 1;
				}

				return 0;
			}

			for (int i = bins - 1; i > 0; i--) {
				current += values[i, channel];
				int nextBin = NextOccupied (values, channel, i - 1, 0, -1);
				if (CloserThanNext (current, values[nextBin, channel], total))
					return current == 0 ? nextBin : i - 1;
			}

			return bins - 1;
		}

		static bool CloserThanNext (double current, int nextCount, double total)
		{
			double percentage = current / total;
			double next = (current + nextCount) / total;
			return Math.Abs (percentage - ClipFraction) < Math.Abs (next - ClipFraction);
		}

		static int NextOccupied (int[,] values, int channel, int start, int last, int step)
		{
			int i = start;
			while (i != last && values[i, channel] == 0)
				i += step;
			return i;
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

		public static byte[] RenderBgra (int[,] values, int width, int height,
			byte backgroundRed = 0, byte backgroundGreen = 0, byte backgroundBlue = 0, byte backgroundAlpha = 255)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));
			if (height <= 0)
				throw new ArgumentOutOfRangeException (nameof (height));
			if (width <= 0)
				throw new ArgumentOutOfRangeException (nameof (width));

			int bins = Math.Min (256, values.GetLength (0));
			long max = 1;
			for (int x = 0; x < width; x++) {
				ColumnRange (bins, width, x, out int start, out int end);
				max = Math.Max (max, Aggregate (values, Red, start, end));
				max = Math.Max (max, Aggregate (values, Green, start, end));
				max = Math.Max (max, Aggregate (values, Blue, start, end));
			}

			var pixels = new byte[width * height * 4];
			for (int x = 0; x < width; x++) {
				ColumnRange (bins, width, x, out int start, out int end);
				int r = BarHeight (Aggregate (values, Red, start, end), height, max);
				int g = BarHeight (Aggregate (values, Green, start, end), height, max);
				int b = BarHeight (Aggregate (values, Blue, start, end), height, max);
				int top = Math.Max (r, Math.Max (g, b));
				for (int y = 0; y < height; y++) {
					int fromBottom = height - 1 - y;
					int i = (y * width + x) * 4;
					if (fromBottom >= top) {
						pixels[i] = backgroundBlue;
						pixels[i + 1] = backgroundGreen;
						pixels[i + 2] = backgroundRed;
						pixels[i + 3] = backgroundAlpha;
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

		// Columns partition the bins when the chart is narrower than 256, and repeat a bin when
		// it is wider. Sampling one bin per column drops codes, including the clip at 255.
		static void ColumnRange (int bins, int width, int x, out int start, out int end)
		{
			start = x * bins / width;
			end = (x + 1) * bins / width;
			if (end <= start)
				end = Math.Min (bins, start + 1);
		}

		static long Aggregate (int[,] values, int channel, int start, int end)
		{
			long sum = 0;
			for (int bin = start; bin < end; bin++)
				sum += values[bin, channel];
			return sum;
		}

		static int BarHeight (long count, int height, long max)
			=> (int)(count * height / max);
	}
}
