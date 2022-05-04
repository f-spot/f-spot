//
// FullColorAdjustment.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gdk;

namespace FSpot.ColorAdjustment
{
	public class FullColorAdjustment : Adjustment
	{
		readonly double exposure;
		readonly double brightness;
		readonly double contrast;
		readonly double hue;
		readonly double saturation;

		Cms.ColorCIEXYZ src_wp;
		Cms.ColorCIEXYZ dest_wp;

		public FullColorAdjustment (Pixbuf input, Cms.Profile input_profile,
				double exposure, double brightness, double contrast,
				double hue, double saturation,
				Cms.ColorCIEXYZ src_wp, Cms.ColorCIEXYZ dest_wp)
			: base (input, input_profile)
		{
			this.exposure = exposure;
			this.brightness = brightness;
			this.contrast = contrast;
			this.hue = hue;
			this.saturation = saturation;
			this.src_wp = src_wp;
			this.dest_wp = dest_wp;
		}

		protected override List<Cms.Profile> GenerateAdjustments ()
		{
			var profiles = new List<Cms.Profile> ();
			profiles.Add (InputProfile);
			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
						Math.Pow (Math.Sqrt (2.0), exposure),
						brightness,
						contrast,
						hue,
						saturation,
						null,
						src_wp.ToxyY (),
						dest_wp.ToxyY ()));
			profiles.Add (DestinationProfile);
			return profiles;
		}
	}
}
