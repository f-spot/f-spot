// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Imaging;

namespace FSpot.ColorAdjustment
{
	public class Desaturate : Adjustment
	{
		public Desaturate (ImageBuffer input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			return new List<Profile> {
				InputProfile,
				Profile.CreateAbstract (nsteps, 1.0, 0.0, 0.0, 0.0, -100.0, null, ColorCIExyY.D50, ColorCIExyY.D50),
				DestinationProfile
			};
		}
	}
}
