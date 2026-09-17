// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Imaging;

namespace FSpot.ColorAdjustment
{
	public abstract class Adjustment
	{
		protected int nsteps = 20;
		readonly Intent intent = Intent.Perceptual;

		protected readonly ImageBuffer Input;

		Profile input_profile;
		public Profile InputProfile {
			get {
				if (input_profile == null)
					input_profile = Profile.CreateStandardRgb ();
				return input_profile;
			}
			set { input_profile = value; }
		}

		Profile destination_profile;
		public Profile DestinationProfile {
			get {
				if (destination_profile == null)
					destination_profile = InputProfile;
				return destination_profile;
			}
			set { destination_profile = value; }
		}

		protected Adjustment (ImageBuffer input, Profile inputProfile)
		{
			Input = input ?? throw new ArgumentNullException (nameof (input));
			InputProfile = inputProfile;
		}

		protected abstract List<Profile> GenerateAdjustments ();

		public ImageBuffer Adjust ()
		{
			Profile[] list = GenerateAdjustments ().ToArray ();
			try {
				return Apply (list);
			} finally {
				foreach (var profile in list)
					if (profile != InputProfile && profile != DestinationProfile)
						profile.Dispose ();
			}
		}

		ImageBuffer Apply (Profile[] list)
		{
			int width = Input.Width;
			int height = Input.Height;
			byte[] rgb = Input.HasAlpha ? FlattenToRgb (Input) : CopyRgb (Input);
			int stride = width * 3;
			var dest = new byte[stride * height];
			using var transform = new Transform (list, Format.Rgb8, Format.Rgb8, intent, 0);
			ApplyRows (transform, rgb, dest, width, height, stride);
			if (!Input.HasAlpha)
				return new ImageBuffer (width, height, false, stride, dest);

			return ReplaceRgbKeepingAlpha (Input, dest);
		}

		static void ApplyRows (Transform transform, byte[] source, byte[] dest, int width, int height, int stride)
		{
			unsafe {
				fixed (byte* srcPtr = source)
				fixed (byte* destPtr = dest) {
					for (int row = 0; row < height; row++) {
						transform.Apply ((IntPtr)(srcPtr + row * stride),
							(IntPtr)(destPtr + row * stride), (uint)width);
					}
				}
			}
		}

		static byte[] CopyRgb (ImageBuffer buffer)
		{
			int stride = buffer.Width * 3;
			var rgb = new byte[stride * buffer.Height];
			int channels = ImageBuffers.Channels (buffer);
			for (int y = 0; y < buffer.Height; y++) {
				int srcRow = y * buffer.Rowstride;
				int dstRow = y * stride;
				if (channels == 3 && buffer.Rowstride == stride) {
					Buffer.BlockCopy (buffer.Pixels, srcRow, rgb, dstRow, stride);
					continue;
				}

				for (int x = 0; x < buffer.Width; x++) {
					int src = srcRow + x * channels;
					int dst = dstRow + x * 3;
					rgb[dst] = buffer.Pixels[src];
					rgb[dst + 1] = buffer.Pixels[src + 1];
					rgb[dst + 2] = buffer.Pixels[src + 2];
				}
			}

			return rgb;
		}

		static byte[] FlattenToRgb (ImageBuffer buffer)
		{
			int stride = buffer.Width * 3;
			var rgb = new byte[stride * buffer.Height];
			for (int y = 0; y < buffer.Height; y++) {
				int srcRow = y * buffer.Rowstride;
				int dstRow = y * stride;
				for (int x = 0; x < buffer.Width; x++) {
					int src = srcRow + x * 4;
					int dst = dstRow + x * 3;
					int a = buffer.Pixels[src + 3];
					int na = 255 - a;
					rgb[dst] = (byte)((buffer.Pixels[src] * a + 255 * na) / 255);
					rgb[dst + 1] = (byte)((buffer.Pixels[src + 1] * a + 255 * na) / 255);
					rgb[dst + 2] = (byte)((buffer.Pixels[src + 2] * a + 255 * na) / 255);
				}
			}

			return rgb;
		}

		static ImageBuffer ReplaceRgbKeepingAlpha (ImageBuffer original, byte[] rgb)
		{
			var output = ImageBuffers.Clone (original);
			int stride = original.Width * 3;
			for (int y = 0; y < original.Height; y++) {
				int dstRow = y * original.Rowstride;
				int srcRow = y * stride;
				for (int x = 0; x < original.Width; x++) {
					int dst = dstRow + x * 4;
					int src = srcRow + x * 3;
					output.Pixels[dst] = rgb[src];
					output.Pixels[dst + 1] = rgb[src + 1];
					output.Pixels[dst + 2] = rgb[src + 2];
				}
			}

			return output;
		}
	}
}
