// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using TagLib.Image;

namespace FSpot.Imaging
{
	public static class ImageBuffers
	{
		public static int Channels (ImageBuffer buffer)
			=> buffer.HasAlpha ? 4 : 3;

		public static ImageBuffer Clone (ImageBuffer buffer)
		{
			var pixels = new byte[buffer.Pixels.Length];
			Buffer.BlockCopy (buffer.Pixels, 0, pixels, 0, pixels.Length);
			return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, buffer.Rowstride, pixels);
		}

		public static ImageBuffer Crop (ImageBuffer buffer, PixelRect selection)
		{
			selection = Clamp (buffer, selection);
			int channels = Channels (buffer);
			int stride = selection.Width * channels;
			var pixels = new byte[stride * selection.Height];
			for (int y = 0; y < selection.Height; y++) {
				int src = (selection.Y + y) * buffer.Rowstride + selection.X * channels;
				Buffer.BlockCopy (buffer.Pixels, src, pixels, y * stride, stride);
			}

			return new ImageBuffer (selection.Width, selection.Height, buffer.HasAlpha, stride, pixels);
		}

		public static void Fill (ImageBuffer buffer, PixelRect selection, byte r, byte g, byte b)
		{
			selection = Clamp (buffer, selection);
			int channels = Channels (buffer);
			for (int y = 0; y < selection.Height; y++) {
				int row = (selection.Y + y) * buffer.Rowstride + selection.X * channels;
				for (int x = 0; x < selection.Width; x++) {
					int i = row + x * channels;
					buffer.Pixels[i] = r;
					buffer.Pixels[i + 1] = g;
					buffer.Pixels[i + 2] = b;
				}
			}
		}

		public static ImageBuffer FlipHorizontal (ImageBuffer buffer)
		{
			var output = Clone (buffer);
			int channels = Channels (buffer);
			int width = buffer.Width;
			for (int y = 0; y < buffer.Height; y++) {
				int row = y * buffer.Rowstride;
				for (int x = 0; x < width / 2; x++) {
					int left = row + x * channels;
					int right = row + (width - 1 - x) * channels;
					for (int c = 0; c < channels; c++) {
						byte tmp = output.Pixels[left + c];
						output.Pixels[left + c] = output.Pixels[right + c];
						output.Pixels[right + c] = tmp;
					}
				}
			}

			return output;
		}

		public static ImageBuffer ScaleAreaAverage (ImageBuffer buffer, int width, int height)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (width <= 0 || height <= 0)
				throw new ArgumentOutOfRangeException (width <= 0 ? nameof (width) : nameof (height));
			if (width == buffer.Width && height == buffer.Height)
				return Clone (buffer);

			int channels = Channels (buffer);
			int stride = width * channels;
			var pixels = new byte[stride * height];
			var sum = new double[channels];
			for (int y = 0; y < height; y++) {
				double y0 = (double)y * buffer.Height / height;
				double y1 = (double)(y + 1) * buffer.Height / height;
				for (int x = 0; x < width; x++) {
					double x0 = (double)x * buffer.Width / width;
					double x1 = (double)(x + 1) * buffer.Width / width;
					AverageCovered (buffer, x0, y0, x1, y1, sum, pixels, (y * width + x) * channels, channels);
				}
			}

			return new ImageBuffer (width, height, buffer.HasAlpha, stride, pixels);
		}

		static void AverageCovered (ImageBuffer source, double x0, double y0, double x1, double y1, double[] sum, byte[] dest, int destIndex, int channels)
		{
			for (int c = 0; c < channels; c++)
				sum[c] = 0;
			int ix0 = Math.Max (0, (int)Math.Floor (x0));
			int iy0 = Math.Max (0, (int)Math.Floor (y0));
			int ix1 = Math.Min (source.Width, (int)Math.Ceiling (x1));
			int iy1 = Math.Min (source.Height, (int)Math.Ceiling (y1));
			double weight = 0;
			for (int y = iy0; y < iy1; y++) {
				double wy = Math.Min (y1, y + 1) - Math.Max (y0, y);
				if (wy <= 0)
					continue;
				int row = y * source.Rowstride;
				for (int x = ix0; x < ix1; x++) {
					double wx = Math.Min (x1, x + 1) - Math.Max (x0, x);
					if (wx <= 0)
						continue;
					double covered = wx * wy;
					weight += covered;
					int pixel = row + x * channels;
					for (int c = 0; c < channels; c++)
						sum[c] += source.Pixels[pixel + c] * covered;
				}
			}

			if (weight <= 0) {
				SampleBilinear (source, (x0 + x1) / 2, (y0 + y1) / 2, dest, destIndex, channels);
				return;
			}

			for (int c = 0; c < channels; c++)
				dest[destIndex + c] = (byte)Math.Round (sum[c] / weight);
		}

		public static ImageBuffer ScaleNearest (ImageBuffer buffer, int width, int height)
		{
			if (width <= 0 || height <= 0)
				throw new ArgumentOutOfRangeException (width <= 0 ? nameof (width) : nameof (height));

			int channels = Channels (buffer);
			int stride = width * channels;
			var pixels = new byte[stride * height];
			for (int y = 0; y < height; y++) {
				int srcY = Math.Min (buffer.Height - 1, y * buffer.Height / height);
				int srcRow = srcY * buffer.Rowstride;
				int dstRow = y * stride;
				for (int x = 0; x < width; x++) {
					int srcX = Math.Min (buffer.Width - 1, x * buffer.Width / width);
					int src = srcRow + srcX * channels;
					int dst = dstRow + x * channels;
					for (int c = 0; c < channels; c++)
						pixels[dst + c] = buffer.Pixels[src + c];
				}
			}

			return new ImageBuffer (width, height, buffer.HasAlpha, stride, pixels);
		}

		public static void Blit (ImageBuffer dest, PixelRect destRect, ImageBuffer source)
		{
			destRect = Clamp (dest, destRect);
			int channels = Channels (dest);
			int srcChannels = Channels (source);
			if (channels != srcChannels)
				throw new ArgumentException ("channel mismatch");

			int copyWidth = Math.Min (destRect.Width, source.Width);
			int copyHeight = Math.Min (destRect.Height, source.Height);
			int bytes = copyWidth * channels;
			for (int y = 0; y < copyHeight; y++) {
				int src = y * source.Rowstride;
				int dst = (destRect.Y + y) * dest.Rowstride + destRect.X * channels;
				Buffer.BlockCopy (source.Pixels, src, dest.Pixels, dst, bytes);
			}
		}

		public static PixelRect Clamp (ImageBuffer buffer, PixelRect selection)
		{
			// Intersect with the image. A negative origin is trimmed, not shifted onto the picture.
			int x0 = Math.Max (0, selection.X);
			int y0 = Math.Max (0, selection.Y);
			int x1 = Math.Min (buffer.Width, selection.X + selection.Width);
			int y1 = Math.Min (buffer.Height, selection.Y + selection.Height);
			return new PixelRect (x0, y0, Math.Max (0, x1 - x0), Math.Max (0, y1 - y0));
		}

		// Pixel transform matching GTK PixbufUtils.TransformOrientation so both
		// clients display EXIF orientation the same way (rotate writes the tag;
		// decode must apply it).
		public static ImageBuffer TransformOrientation (ImageBuffer source, ImageOrientation orientation)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			switch (orientation) {
			case ImageOrientation.TopRight:
				return FlipVertical (source);
			case ImageOrientation.BottomRight:
				return FlipHorizontal (FlipVertical (source));
			case ImageOrientation.BottomLeft:
				return FlipHorizontal (source);
			case ImageOrientation.LeftTop:
				return FlipVertical (Rotate90Clockwise (source));
			case ImageOrientation.RightTop:
				return Rotate90Clockwise (source);
			case ImageOrientation.RightBottom:
				return FlipVertical (Rotate90Counterclockwise (source));
			case ImageOrientation.LeftBottom:
				return Rotate90Counterclockwise (source);
			default:
				return source;
			}
		}

		public static ImageBuffer FlipVertical (ImageBuffer buffer)
		{
			int channels = Channels (buffer);
			int stride = buffer.Width * channels;
			var pixels = new byte[stride * buffer.Height];
			for (int y = 0; y < buffer.Height; y++) {
				int src = (buffer.Height - 1 - y) * buffer.Rowstride;
				Buffer.BlockCopy (buffer.Pixels, src, pixels, y * stride, stride);
			}

			return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, stride, pixels);
		}

		public static ImageBuffer Rotate90Clockwise (ImageBuffer buffer)
		{
			int channels = Channels (buffer);
			int width = buffer.Height;
			int height = buffer.Width;
			int stride = width * channels;
			var pixels = new byte[stride * height];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					CopyPixel (buffer, y, buffer.Height - 1 - x, pixels, y * stride + x * channels, channels);
				}
			}

			return new ImageBuffer (width, height, buffer.HasAlpha, stride, pixels);
		}

		public static ImageBuffer Rotate90Counterclockwise (ImageBuffer buffer)
		{
			int channels = Channels (buffer);
			int width = buffer.Height;
			int height = buffer.Width;
			int stride = width * channels;
			var pixels = new byte[stride * height];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					CopyPixel (buffer, buffer.Width - 1 - y, x, pixels, y * stride + x * channels, channels);
				}
			}

			return new ImageBuffer (width, height, buffer.HasAlpha, stride, pixels);
		}

		static void CopyPixel (ImageBuffer source, int srcX, int srcY, byte[] dest, int destIndex, int channels)
		{
			int src = srcY * source.Rowstride + srcX * channels;
			for (int c = 0; c < channels; c++)
				dest[destIndex + c] = source.Pixels[src + c];
		}

		// GTK ImageInfo.Fill (viewport, tilt): scale so a rotated copy still
		// covers the frame, matching straighten.
		public static double TiltCoverScale (int width, int height, double tiltRadians)
		{
			if (width <= 0 || height <= 0)
				throw new ArgumentOutOfRangeException (width <= 0 ? nameof (width) : nameof (height));
			if (tiltRadians == 0.0)
				return 1.0;

			double len;
			double origLen;
			if (width > height) {
				len = height;
				origLen = height;
			} else {
				len = width;
				origLen = width;
			}

			double a = Math.Sqrt (width * (double)width + height * (double)height);
			double alpha = Math.Acos (len / a);
			double theta = alpha - Math.Abs (tiltRadians);
			return a * Math.Cos (theta) / origLen;
		}

		public static ImageBuffer TiltFill (ImageBuffer buffer, double degrees)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (degrees == 0)
				return Clone (buffer);

			// Cairo rotates a positive angle clockwise when y grows downward.
			double tilt = degrees * Math.PI / 180.0;
			double scale = TiltCoverScale (buffer.Width, buffer.Height, tilt);
			double cos = Math.Cos (tilt);
			double sin = Math.Sin (tilt);
			double cx = (buffer.Width - 1) / 2.0;
			double cy = (buffer.Height - 1) / 2.0;
			int channels = Channels (buffer);
			int stride = buffer.Width * channels;
			var pixels = new byte[stride * buffer.Height];
			for (int y = 0; y < buffer.Height; y++) {
				int dstRow = y * stride;
				double vy = y - cy;
				for (int x = 0; x < buffer.Width; x++) {
					double vx = x - cx;
					double sx = (cos * vx + sin * vy) / scale + cx;
					double sy = (-sin * vx + cos * vy) / scale + cy;
					SampleBilinear (buffer, sx, sy, pixels, dstRow + x * channels, channels);
				}
			}

			return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, stride, pixels);
		}

		// GTK SoftFocus: downscale to ~256, Gaussian-ish blur (Amount=3),
		// then radial-mask the blur over the original.
		public static ImageBuffer SoftFocus (ImageBuffer buffer, double radius)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (radius < 0)
				radius = 0;

			double fit = Math.Max (256.0 / buffer.Width, 256.0 / buffer.Height);
			int blurWidth = Math.Max (1, (int)Math.Ceiling (buffer.Width * fit));
			int blurHeight = Math.Max (1, (int)Math.Ceiling (buffer.Height * fit));
			var small = ScaleAreaAverage (buffer, blurWidth, blurHeight);
			var blurred = BoxBlur (small, 3);

			int channels = Channels (buffer);
			int stride = buffer.Width * channels;
			var pixels = new byte[stride * buffer.Height];
			double max = Math.Max (blurWidth, blurHeight) * 0.25;
			double inner = radius * max * 0.7;
			double outer = radius * max + max * 0.2;
			double span = Math.Max (1e-6, outer - inner);
			double scaleX = blurWidth / (double)buffer.Width;
			double scaleY = blurHeight / (double)buffer.Height;
			double cx = (buffer.Width / 2) * scaleX;
			double cy = (buffer.Height / 2) * scaleY;
			var sample = new byte[channels];
			for (int y = 0; y < buffer.Height; y++) {
				int dstRow = y * stride;
				int srcRow = y * buffer.Rowstride;
				double by = y * scaleY;
				for (int x = 0; x < buffer.Width; x++) {
					double bx = x * scaleX;
					double dx = bx - cx;
					double dy = by - cy;
					double dist = Math.Sqrt (dx * dx + dy * dy);
					double t = (dist - inner) / span;
					if (t < 0)
						t = 0;
					else if (t > 1)
						t = 1;
					int src = srcRow + x * channels;
					int dst = dstRow + x * channels;
					SampleBilinear (blurred, bx, by, sample, 0, channels);
					for (int c = 0; c < channels; c++) {
						double orig = buffer.Pixels[src + c];
						double blur = sample[c];
						pixels[dst + c] = (byte)Math.Round (orig + (blur - orig) * t);
					}
				}
			}

			return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, stride, pixels);
		}

		// GTK PixbufUtils.UnsharpMask: blur, then orig + amount * (orig - blur)
		// unless |2 * diff| < threshold.
		public static ImageBuffer UnsharpMask (ImageBuffer buffer, double radius, double amount, double threshold)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));

			int blurRadius = Math.Max (0, (int)radius);
			var blurred = PeakedBlur (buffer, blurRadius);
			int channels = Channels (buffer);
			int stride = buffer.Width * channels;
			var pixels = new byte[stride * buffer.Height];
			for (int y = 0; y < buffer.Height; y++) {
				int srcRow = y * buffer.Rowstride;
				int blurRow = y * blurred.Rowstride;
				int dstRow = y * stride;
				for (int x = 0; x < buffer.Width; x++) {
					int src = srcRow + x * channels;
					int blur = blurRow + x * channels;
					int dst = dstRow + x * channels;
					for (int c = 0; c < channels; c++) {
						int orig = buffer.Pixels[src + c];
						int diff = orig - blurred.Pixels[blur + c];
						if (Math.Abs (2 * diff) < threshold)
							diff = 0;
						int val = (int)(orig + amount * diff);
						if (val > 255)
							val = 255;
						else if (val < 0)
							val = 0;
						pixels[dst + c] = (byte)val;
					}
				}
			}

			return new ImageBuffer (buffer.Width, buffer.Height, buffer.HasAlpha, stride, pixels);
		}

		// GTK GaussianBlurEffect.CreateGaussianBlurRow: a tent, heavier at the center than a box.
		static ImageBuffer PeakedBlur (ImageBuffer buffer, int radius)
		{
			if (radius <= 0)
				return Clone (buffer);

			int size = 1 + radius * 2;
			var weights = new int[size];
			int weightSum = 0;
			for (int i = 0; i <= radius; i++) {
				int weight = 16 * (i + 1);
				weights[i] = weight;
				weights[size - i - 1] = weight;
				weightSum += weight;
				if (i != radius)
					weightSum += weight;
			}

			int channels = Channels (buffer);
			var horizontal = Clone (buffer);
			for (int y = 0; y < buffer.Height; y++) {
				int row = y * buffer.Rowstride;
				for (int x = 0; x < buffer.Width; x++) {
					for (int c = 0; c < channels; c++) {
						int sum = 0;
						for (int k = 0; k < size; k++) {
							int sx = x + k - radius;
							if (sx < 0)
								sx = 0;
							else if (sx >= buffer.Width)
								sx = buffer.Width - 1;
							sum += buffer.Pixels[row + sx * channels + c] * weights[k];
						}

						horizontal.Pixels[row + x * channels + c] = (byte)(sum / weightSum);
					}
				}
			}

			var vertical = Clone (horizontal);
			for (int y = 0; y < buffer.Height; y++) {
				for (int x = 0; x < buffer.Width; x++) {
					for (int c = 0; c < channels; c++) {
						int sum = 0;
						for (int k = 0; k < size; k++) {
							int sy = y + k - radius;
							if (sy < 0)
								sy = 0;
							else if (sy >= buffer.Height)
								sy = buffer.Height - 1;
							sum += horizontal.Pixels[sy * horizontal.Rowstride + x * channels + c] * weights[k];
						}

						vertical.Pixels[y * vertical.Rowstride + x * channels + c] = (byte)(sum / weightSum);
					}
				}
			}

			return vertical;
		}

		static ImageBuffer BoxBlur (ImageBuffer buffer, int radius)
		{
			if (radius <= 0)
				return Clone (buffer);

			var horizontal = Clone (buffer);
			int channels = Channels (buffer);
			int diameter = radius * 2 + 1;
			for (int y = 0; y < buffer.Height; y++) {
				int row = y * buffer.Rowstride;
				for (int x = 0; x < buffer.Width; x++) {
					for (int c = 0; c < channels; c++) {
						int sum = 0;
						for (int k = -radius; k <= radius; k++) {
							int sx = x + k;
							if (sx < 0)
								sx = 0;
							else if (sx >= buffer.Width)
								sx = buffer.Width - 1;
							sum += buffer.Pixels[row + sx * channels + c];
						}

						horizontal.Pixels[row + x * channels + c] = (byte)(sum / diameter);
					}
				}
			}

			var vertical = Clone (horizontal);
			for (int y = 0; y < buffer.Height; y++) {
				for (int x = 0; x < buffer.Width; x++) {
					for (int c = 0; c < channels; c++) {
						int sum = 0;
						for (int k = -radius; k <= radius; k++) {
							int sy = y + k;
							if (sy < 0)
								sy = 0;
							else if (sy >= buffer.Height)
								sy = buffer.Height - 1;
							sum += horizontal.Pixels[sy * horizontal.Rowstride + x * channels + c];
						}

						vertical.Pixels[y * vertical.Rowstride + x * channels + c] = (byte)(sum / diameter);
					}
				}
			}

			return vertical;
		}

		static void SampleBilinear (ImageBuffer source, double x, double y, byte[] dest, int destIndex, int channels)
		{
			if (x < 0)
				x = 0;
			else if (x > source.Width - 1)
				x = source.Width - 1;
			if (y < 0)
				y = 0;
			else if (y > source.Height - 1)
				y = source.Height - 1;

			int x0 = (int)x;
			int y0 = (int)y;
			int x1 = Math.Min (source.Width - 1, x0 + 1);
			int y1 = Math.Min (source.Height - 1, y0 + 1);
			double fx = x - x0;
			double fy = y - y0;
			int row0 = y0 * source.Rowstride;
			int row1 = y1 * source.Rowstride;
			int i00 = row0 + x0 * channels;
			int i10 = row0 + x1 * channels;
			int i01 = row1 + x0 * channels;
			int i11 = row1 + x1 * channels;
			for (int c = 0; c < channels; c++) {
				double v0 = source.Pixels[i00 + c] * (1 - fx) + source.Pixels[i10 + c] * fx;
				double v1 = source.Pixels[i01 + c] * (1 - fx) + source.Pixels[i11 + c] * fx;
				dest[destIndex + c] = (byte)Math.Round (v0 * (1 - fy) + v1 * fy);
			}
		}
	}
}
