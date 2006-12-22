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

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot.Filters {
	public class ColorFilter : IFilter {
		Cms.Profile adjustment;
		Cms.Profile destination;
		Cms.Intent rendering_intent;

		public ColorFilter (Cms.Profile adjustment)
		{
			this.adjustment = adjustment;
		}

#if false		
                // TODO FIXME until we support saving a new profile to the image we can't
	        // really allow people to set the destination profile.  Remember to remove
		// all the extra metadata that might confuse the profile.
		public Cms.Profile Output {
			set { destination = value; }
		}
#endif

		public Cms.Profile Adjustment {
			get { return adjustment; }
			set { adjustment = value; }
		}

		public Cms.Intent RenderingIntent {
			set { rendering_intent = value; }
			get { return rendering_intent; }
		}

		public bool Convert (FilterRequest req)
		{
			Uri source = req.Current;
			ImageFile img = ImageFile.Create (source);
			Gdk.Pixbuf pixbuf = img.Load ();
			
			Cms.Profile profile = img.GetProfile ();

			// If the image doesn't have an embedded profile assume it is sRGB
			if (profile == null)
				profile = Cms.Profile.CreateStandardRgb ();

			if (destination == null)
				destination = profile;

			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   pixbuf.Width, 
							   pixbuf.Height);
			Cms.Profile [] list;
			if (adjustment != null)
				list = new Cms.Profile [] { profile, adjustment, destination };
			else
				list = new Cms.Profile [] { profile, destination };

			Cms.Transform transform = new Cms.Transform (list,
								     PixbufUtils.PixbufCmsFormat (pixbuf),
								     PixbufUtils.PixbufCmsFormat (final),
								     rendering_intent, 0x0000);
			
			PixbufUtils.ColorAdjust (pixbuf, final, transform);
			if (pixbuf.HasAlpha) {
				// FIXME this is a hack to deal with the alpha channel since
				// lcms has issues with it.
				PixbufUtils.ReplaceColor (final, pixbuf);
				final.Dispose ();
				final = pixbuf;
			} else {
				pixbuf.Dispose ();
			}

			req.TempUri (Path.GetExtension (source.LocalPath));
			using (Stream output = File.OpenWrite (req.Current.LocalPath)) {
				img.Save (final, output);
			}

			final.Dispose ();

			return true;
		}

#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest {
			[Test]
			public void PngPass ()
			{
				PassThrough ("passthrough.png");
			}
			
			public void PassThrough (string name)
			{
				string path = CreateFile (name, 120);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new ColorFilter (null);
					filter.Convert (req);
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
