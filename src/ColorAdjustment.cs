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

		public Gdk.Pixbuf Pixbuf {
			get { return image; }
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
			using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri)) {
				if (image == null)
					image = img.Load ();
			
				if (image_profile == null)
					image_profile = img.GetProfile ();
			}

			if (image_profile == null)
				image_profile = Cms.Profile.CreateStandardRgb ();

			if (destination_profile == null)
				destination_profile = image_profile;

			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   image.Width, 
							   image.Height);
			profiles = new List <Cms.Profile> (4);
			profiles.Add (image_profile);
			GenerateAdjustments ();
			profiles.Add (destination_profile);
			Cms.Profile [] list = profiles.ToArray ();
			
			if (image.HasAlpha) {
				Pixbuf alpha = PixbufUtils.Flatten (image);
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (alpha),
								     PixbufUtils.PixbufCmsFormat (final),
								     intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, image);
				alpha.Dispose ();
				final.Dispose ();
				final = image;
			} else {
				Cms.Transform transform = new Cms.Transform (list,
									     PixbufUtils.PixbufCmsFormat (image),
									     PixbufUtils.PixbufCmsFormat (final),
									     intent, 0x0000);
				
				PixbufUtils.ColorAdjust (image, final, transform);
				image.Dispose ();
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
