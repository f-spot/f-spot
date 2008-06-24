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
		protected Photo photo;
		protected List <Cms.Profile> profiles;
		protected Cms.Profile image_profile;
		protected Cms.Profile destination_profile;
		protected Cms.Profile adjustment_profile;
		protected Gdk.Pixbuf image;
		protected int nsteps = 20;
		protected Cms.Intent intent = Cms.Intent.Perceptual;

		// This is the input pixbuf, on which the adjustment will be performed.
		//
		// If it is not assigned, it will be loaded from the photo given when
		// constructing the ColorAdjustment. However, assigning it (if you
		// already have a copy in memory) avoids doing a duplicate load.
		public Gdk.Pixbuf Image {
			get {
				if (image == null) {
					using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri)) {
						image = img.Load ();

						if (image_profile == null)
							image_profile = img.GetProfile ();
					}
				}
				return image;
			}
			set { image = value; }
		}

		public ColorAdjustment (Photo photo)
		{
			this.photo = photo;
		}
		
		public void SetDestination (Cms.Profile profile)
		{
			destination_profile = profile;
		}

		protected abstract void GenerateAdjustments ();

		public void Adjust ()
		{
			bool create_version = photo.DefaultVersion.IsProtected;

			if (image_profile == null)
				image_profile = Cms.Profile.CreateStandardRgb ();

			if (destination_profile == null)
				destination_profile = image_profile;

			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   Image.Width,
							   Image.Height);
			profiles = new List <Cms.Profile> (4);
			profiles.Add (image_profile);
			GenerateAdjustments ();
			profiles.Add (destination_profile);
			Cms.Profile [] list = profiles.ToArray ();
			
			if (Image.HasAlpha) {
				Pixbuf alpha = PixbufUtils.Flatten (Image);
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (alpha),
								     PixbufUtils.PixbufCmsFormat (final),
								     intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, Image);
				alpha.Dispose ();
				final.Dispose ();
				final = Image;
			} else {
				Cms.Transform transform = new Cms.Transform (list,
									     PixbufUtils.PixbufCmsFormat (Image),
									     PixbufUtils.PixbufCmsFormat (final),
									     intent, 0x0000);
				
				PixbufUtils.ColorAdjust (Image, final, transform);
				Image.Dispose ();
			}
				
			photo.SaveVersion (final, create_version);
			final.Dispose ();
		}
	}

	public class SepiaTone : ColorAdjustment {
		public SepiaTone (Photo photo) : base (photo)
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
		public Desaturate (Photo photo) : base (photo)
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
