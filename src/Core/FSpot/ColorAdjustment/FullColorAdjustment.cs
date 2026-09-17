// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Imaging;

namespace FSpot.ColorAdjustment
{
	public class FullColorAdjustment : Adjustment
	{
		readonly double exposure;
		readonly double brightness;
		readonly double contrast;
		readonly double hue;
		readonly double saturation;
		readonly ColorCIEXYZ srcWp;
		readonly ColorCIEXYZ destWp;

		public FullColorAdjustment (ImageBuffer input, Profile inputProfile,
			double exposure, double brightness, double contrast,
			double hue, double saturation,
			ColorCIEXYZ srcWp, ColorCIEXYZ destWp)
			: base (input, inputProfile)
		{
			this.exposure = exposure;
			this.brightness = brightness;
			this.contrast = contrast;
			this.hue = hue;
			this.saturation = saturation;
			this.srcWp = srcWp;
			this.destWp = destWp;
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			return new List<Profile> {
				InputProfile,
				Profile.CreateAbstract (nsteps,
					Math.Pow (Math.Sqrt (2.0), exposure),
					brightness, contrast, hue, saturation, null,
					srcWp.ToxyY (), destWp.ToxyY ()),
				DestinationProfile
			};
		}
	}
}
