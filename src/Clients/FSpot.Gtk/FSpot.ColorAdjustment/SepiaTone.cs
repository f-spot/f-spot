//
// SepiaTone.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Cms;

using Gdk;

namespace FSpot.ColorAdjustment
{
	public class SepiaTone : Adjustment
	{
		public SepiaTone (Pixbuf input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			var profiles = new List<Profile> ();
			profiles.Add (InputProfile);
			profiles.Add (Profile.CreateAbstract (nsteps,
								  1.0,
								  0.0,
								  0.0,
								  0.0,
								  -100.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.D50));

			profiles.Add (Profile.CreateAbstract (nsteps,
								  1.0,
								  32.0,
								  0.0,
								  0.0,
								  0.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.WhitePointFromTemperature (9934)));
			profiles.Add (DestinationProfile);
			return profiles;
		}
	}
}
