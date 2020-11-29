//
// Adjustment.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Utils;

using Gdk;

namespace FSpot.ColorAdjustment
{
	public abstract class Adjustment
	{
		protected int nsteps = 20;
		Cms.Intent intent = Cms.Intent.Perceptual;

		// This is the input pixbuf, on which the adjustment will be performed.
		protected readonly Gdk.Pixbuf Input;

		Cms.Profile input_profile;
		public Cms.Profile InputProfile {
			get {
				if (input_profile == null)
					input_profile = Cms.Profile.CreateStandardRgb ();

				return input_profile;
			}
			set { input_profile = value; }
		}

		Cms.Profile destination_profile;
		public Cms.Profile DestinationProfile {
			get {
				if (destination_profile == null)
					destination_profile = InputProfile;

				return destination_profile;
			}
			set { destination_profile = value; }
		}

		protected Adjustment (Pixbuf input, Cms.Profile inputProfile)
		{
			Input = input;
			InputProfile = inputProfile;
		}

		protected abstract List<Cms.Profile> GenerateAdjustments ();

		public Pixbuf Adjust ()
		{
			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   Input.Width,
							   Input.Height);
			Cms.Profile[] list = GenerateAdjustments ().ToArray ();

			if (Input.HasAlpha) {
				Gdk.Pixbuf input_copy = (Gdk.Pixbuf)Input.Clone ();
				using var alpha = PixbufUtils.Flatten (Input);
				using var transform = new Transform (list,
									 PixbufUtils.PixbufCmsFormat (alpha),
									 PixbufUtils.PixbufCmsFormat (final),
									 intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, input_copy);
				alpha.Dispose ();
				final.Dispose ();
				final = input_copy;
			} else {
				using var transform = new Cms.Transform (list,
										 PixbufUtils.PixbufCmsFormat (Input),
										 PixbufUtils.PixbufCmsFormat (final),
										 intent, 0x0000);

				PixbufUtils.ColorAdjust (Input, final, transform);
			}

			return final;
		}
	}
}
