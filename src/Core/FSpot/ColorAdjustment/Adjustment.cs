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

		// Alpha stays with the pixel. Flattening onto white first would colour-manage
		// a different colour and then paste the old alpha back on top.
		ImageBuffer Apply (Profile[] list)
		{
			var output = ImageBuffers.Clone (Input);
			var format = output.HasAlpha ? Format.Rgba8 : Format.Rgb8;
			uint flags = CmsTransforms.For (print: false, copyAlpha: output.HasAlpha);
			using var transform = new Transform (list, format, format, intent, flags);
			unsafe {
				fixed (byte* start = output.Pixels) {
					for (int row = 0; row < output.Height; row++) {
						byte* line = start + row * output.Rowstride;
						transform.Apply ((IntPtr)line, (IntPtr)line, (uint)output.Width);
					}
				}
			}

			return output;
		}
	}
}
