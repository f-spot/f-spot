/*
 * Adjustment.cs
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

using FSpot.Utils;
using Cms;
using Gdk;
using System.Collections.Generic;

namespace FSpot.ColorAdjustment {
	public abstract class Adjustment {
		private List <Cms.Profile> profiles;
		protected int nsteps = 20;
		private Cms.Intent intent = Cms.Intent.Perceptual;

		// This is the input pixbuf, on which the adjustment will be performed.
		protected readonly Gdk.Pixbuf Input;

		private Cms.Profile input_profile;
		public Cms.Profile InputProfile {
			get {
				if (input_profile == null)
					input_profile = Cms.Profile.CreateStandardRgb ();

				return input_profile;
			}
			set { input_profile = value; }
		}

		private Cms.Profile destination_profile;
		public Cms.Profile DestinationProfile {
			get {
				if (destination_profile == null)
					destination_profile = InputProfile;

				return destination_profile;
			}
			set { destination_profile = value; }
		}

		public Adjustment (Pixbuf input, Cms.Profile input_profile)
		{
			Input = input;
			InputProfile = input_profile;
		}

		protected abstract List <Cms.Profile> GenerateAdjustments ();

		public Pixbuf Adjust ()
		{
			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   Input.Width,
							   Input.Height);
			Cms.Profile [] list = GenerateAdjustments ().ToArray ();
			
			if (Input.HasAlpha) {
				Pixbuf alpha = PixbufUtils.Flatten (Input);
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (alpha),
								     PixbufUtils.PixbufCmsFormat (final),
								     intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, Input);
				alpha.Dispose ();
				final.Dispose ();
				final = Input;
			} else {
				Cms.Transform transform = new Cms.Transform (list,
									     PixbufUtils.PixbufCmsFormat (Input),
									     PixbufUtils.PixbufCmsFormat (final),
									     intent, 0x0000);
				
				PixbufUtils.ColorAdjust (Input, final, transform);
			}

			return final;
		}
	}
}
