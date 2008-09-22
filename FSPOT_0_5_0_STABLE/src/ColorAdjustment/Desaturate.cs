/*
 * Desaturate.cs
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
using System.Collections.Generic;

namespace FSpot.ColorAdjustment {
	public class Desaturate : Adjustment {
		public Desaturate (Pixbuf input, Cms.Profile input_profile) : base (input, input_profile)
		{
		}

		protected override List <Cms.Profile> GenerateAdjustments ()
		{
			List <Cms.Profile> profiles = new List <Cms.Profile> ();
			profiles.Add (InputProfile);
			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
								  1.0,
								  0.0,
								  0.0,
								  0.0,
								  -100.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.D50));
			profiles.Add (DestinationProfile);
			return profiles;
		}
	}
}
