/*
 * ColorAdjustment.cs
 * 
 * Copyright 2006, 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information
 *
 */
using Cms;
using Gdk;
using System.Collections.Generic;

namespace FSpot {
	public abstract class ColorAdjustment {
		protected List <Cms.Profile> profiles;
		protected Cms.Profile adjustment_profile;
		protected int nsteps = 20;
		protected Cms.Intent intent = Cms.Intent.Perceptual;

		// This is the input pixbuf, on which the adjustment will be performed.
		private readonly Gdk.Pixbuf Input;

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

		public ColorAdjustment (Pixbuf input, Cms.Profile input_profile)
		{
			this.Input = input;
			this.input_profile = input_profile;
		}

		protected abstract void GenerateAdjustments ();

		public Pixbuf Adjust ()
		{
			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   Input.Width,
							   Input.Height);
			profiles = new List <Cms.Profile> (4);
			profiles.Add (InputProfile);
			GenerateAdjustments ();
			profiles.Add (DestinationProfile);
			Cms.Profile [] list = profiles.ToArray ();
			
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
				Input.Dispose ();
			}

			return final;
		}
	}

	public class SepiaTone : ColorAdjustment {
		public SepiaTone (Pixbuf input, Cms.Profile input_profile) : base (input, input_profile)
		{
		}

		protected override void GenerateAdjustments ()
		{
			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
								  1.0,
								  0.0,
								  0.0,
								  0.0,
								  -100.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.D50));

			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
								  1.0,
								  32.0,
								  0.0,
								  0.0,
								  0.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.WhitePointFromTemperature (9934)));
		}
	}

	public class Desaturate : ColorAdjustment {
		public Desaturate (Pixbuf input, Cms.Profile input_profile) : base (input, input_profile)
		{
		}

		protected override void GenerateAdjustments ()
		{
			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
								  1.0,
								  0.0,
								  0.0,
								  0.0,
								  -100.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.D50));
		}
	}
}
