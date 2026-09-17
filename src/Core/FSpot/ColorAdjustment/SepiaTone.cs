// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Imaging;

namespace FSpot.ColorAdjustment
{
	public class SepiaTone : Adjustment
	{
		public SepiaTone (ImageBuffer input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			return new List<Profile> {
				InputProfile,
				Profile.CreateAbstract (nsteps, 1.0, 0.0, 0.0, 0.0, -100.0, null, ColorCIExyY.D50, ColorCIExyY.D50),
				Profile.CreateAbstract (nsteps, 1.0, 32.0, 0.0, 0.0, 0.0, null, ColorCIExyY.D50, ColorCIExyY.WhitePointFromTemperature (9934)),
				DestinationProfile
			};
		}
	}
}
