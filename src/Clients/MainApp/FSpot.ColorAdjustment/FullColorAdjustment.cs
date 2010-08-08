/*
 * FullColorAdjustment.cs
 *
 * Copyright 2006, 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *   Ruben Vermeersch <ruben@savanne.be>
 *
 * See COPYING for license information
 *
 */
using Cms;
using Gdk;
using System;
using System.Collections.Generic;

namespace FSpot.ColorAdjustment {
	public class FullColorAdjustment : Adjustment {
		private double exposure;
		private double brightness;
		private double contrast;
		private double hue;
		private double saturation;
		private Cms.ColorCIEXYZ src_wp;
		private Cms.ColorCIEXYZ dest_wp;

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

		protected override List <Cms.Profile> GenerateAdjustments ()
		{
			List <Cms.Profile> profiles = new List <Cms.Profile> ();
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
