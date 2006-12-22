/*
 * Filters/RotateFilter.cs
 *
 * Author(s)
 *
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using System.IO;
using Gdk;
using Cms;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot.Filters {
	public class ColorFilter : IFilter {
		Profile adjustment;
		Profile destination;
		Intent rendering_intent = Intent.Perceptual;

		public ColorFilter (Profile adjustment)
		{
			this.adjustment = adjustment;
		}

#if false		
                // TODO FIXME until we support saving a new profile to the image we can't
	        // really allow people to set the destination profile.  Remember to remove
		// all the extra metadata that might confuse the profile.
		public Profile Output {
			set { destination = value; }
		}
#endif

		public Profile Adjustment {
			get { return adjustment; }
			set { adjustment = value; }
		}

		public Intent RenderingIntent {
			set { rendering_intent = value; }
			get { return rendering_intent; }
		}

		public bool Convert (FilterRequest req)
		{
			Uri source = req.Current;
			ImageFile img = ImageFile.Create (source);
			Gdk.Pixbuf pixbuf = img.Load ();
			
			Profile profile = img.GetProfile ();

			// If the image doesn't have an embedded profile assume it is sRGB
			if (profile == null)
				profile = Profile.CreateStandardRgb ();

			if (destination == null)
				destination = profile;

			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   pixbuf.Width, 
							   pixbuf.Height);
			Profile [] list;
			if (adjustment != null)
				list = new Profile [] { profile, adjustment, destination };
			else
				list = new Profile [] { profile, destination };

			if (pixbuf.HasAlpha) {
				Gdk.Pixbuf alpha = PixbufUtils.Flatten (pixbuf);
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (alpha),
								     PixbufUtils.PixbufCmsFormat (final),
								     rendering_intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, pixbuf);
				alpha.Dispose ();
				final.Dispose ();
				final = pixbuf;
			} else {
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (pixbuf),
								     PixbufUtils.PixbufCmsFormat (final),
								     rendering_intent, 0x0000);
				PixbufUtils.ColorAdjust (pixbuf, final, transform);
				pixbuf.Dispose ();
			}
			
			Uri dest_uri = req.TempUri (Path.GetExtension (source.LocalPath));
			using (Stream output = File.OpenWrite (dest_uri.LocalPath)) {
				img.Save (final, output);
			}
			final.Dispose ();
			req.Current = dest_uri;
			
			return true;
		}
		
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest {
			[Test]
			public void AlphaPassthrough ()
			{
				Process ("passthrough.png", null);
			}

			[Test]
			public void OpaquePassthrough ()
			{
				Process ("passthrough.jpg", null);
			}

			[Test]
			public void OpaqueDesaturate ()
			{
				Desaturate ("desaturate.jpg");
			}

			[Test]
			public void AlphaDesaturate ()
			{
				Desaturate ("desaturate.png");
			}

			public void Desaturate (string name)
			{
				Profile adjustment = Profile.CreateAbstract (10,
									     1.0,
									     0.0,
									     0.0,
									     0.0,
									     -100.0,
									     null,
									     ColorCIExyY.WhitePointFromTemperature (5000),
									     ColorCIExyY.WhitePointFromTemperature (5000));

				string path = CreateFile (name, 32);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new ColorFilter (adjustment);
					Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
					req.Preserve (req.Current);
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current + "is Zero length");
					ImageFile img = ImageFile.Create (req.Current);
					Pixbuf pixbuf = img.Load ();
					Assert.IsNotNull (pixbuf);
					Assert.IsTrue (PixbufUtils.IsGray (pixbuf, 1), "failed to desaturate " + req.Current);
				}

			}
	
			public void Process (string name, Profile profile)
			{
				string path = CreateFile (name, 120);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new ColorFilter (profile);
					Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current.LocalPath);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current.LocalPath + "is Zero length");
				}
			}
		}
#endif
	}
}
