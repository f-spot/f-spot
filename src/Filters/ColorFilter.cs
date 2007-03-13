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
	public class AutoStretch : ColorFilter {
		public AutoStretch ()
		{
		}

		GammaTable StretchChannel (int count, double low, double high)
		{
			ushort [] entries = new ushort [count];
			for (int i = 0; i < entries.Length; i++) {
				double val = i / (double)entries.Length;
				
				if (high != low) {
					val = Math.Max ((val - low), 0) / (high - low);
				} else {
					val = Math.Max ((val - low), 0);
				}

				entries [i] = (ushort) Math.Min (Math.Round (ushort.MaxValue * val), ushort.MaxValue);
				//System.Console.WriteLine ("val {0}, result {1}", Math.Round (val * ushort.MaxValue), entries [i]);
			}
			return new GammaTable (entries);
		}

		GammaTable [] tables;
		protected override Profile [] Prepare (Gdk.Pixbuf image)
		{
			Histogram hist = new Histogram (image);
			tables = new GammaTable [3];

			for (int channel = 0; channel < tables.Length; channel++) {
				int high, low;
				hist.GetHighLow (channel, out high, out low);
				System.Console.WriteLine ("high = {0}, low = {1}", high, low);
				tables [channel] = StretchChannel (255, low / 255.0, high / 255.0); 
			}

			return new Profile [] { new Profile (IccColorSpace.Rgb, tables) };
		}

#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest
		{
			[Test]
			public void GoGoGadgetStretch ()
			{
				Process ("autostretch.png");
			}

#if false
			[Test]
			public void StretchRealFile ()
			{
				string path = "/home/lewing/Desktop/img_0081.jpg";
				FilterRequest req = new FilterRequest (path);
				FilterSet set = new FilterSet ();
				set.Add (new AutoStretch ());
				set.Add (new UniqueNameFilter (Path.GetDirectoryName (path)));
				set.Convert (req);
				req.Preserve (req.Current);
			}
#endif

			public void Process (string name)
			{
				string path = CreateFile (name, 120);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new AutoStretch ();
					Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current.LocalPath);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current.LocalPath + "is Zero length");
					//req.Preserve (req.Current);
					using (ImageFile img = ImageFile.Create (req.Current)) {
						Pixbuf pixbuf = img.Load ();
						Assert.IsNotNull (pixbuf);
					}
				}
			}
		}
#endif		
	}

	public class ColorFilter : IFilter {
		// Abstract adjustment profile
		protected Profile adjustment;
		// Image Destination profile
		protected Profile destination;
		// Image Profile
		protected Profile profile; 
		// device-link profile
		protected Profile link;
		protected Intent rendering_intent = Intent.Perceptual;
		// Image buffer
		protected Gdk.Pixbuf pixbuf;

		public ColorFilter () : this (null)
		{
		}

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
		public Profile DeviceLink {
			get { return link; }
			set { link = value; }
		}

		public Profile Adjustment {
			get { return adjustment; }
			set { adjustment = value; }
		}

		public Intent RenderingIntent {
			set { rendering_intent = value; }
			get { return rendering_intent; }
		}

		protected virtual Profile [] Prepare (Gdk.Pixbuf image)
		{
			Profile [] list;

			if (link != null)
				list = new Profile [] { link };
			else if (adjustment != null)
				list = new Profile [] { profile, adjustment, destination };
			else
				list = new Profile [] { profile, destination };

			return list;
		}

		public bool Convert (FilterRequest req)
		{
			Uri source = req.Current;
			using (ImageFile img = ImageFile.Create (source)) {
				pixbuf = img.Load ();
				profile = img.GetProfile ();
	
				// If the image doesn't have an embedded profile assume it is sRGB
				if (profile == null)
					profile = Profile.CreateStandardRgb ();
	
				if (destination == null)
					destination = profile;
	
				Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
								   false, 8,
								   pixbuf.Width, 
								   pixbuf.Height);
	
				Profile [] list = Prepare (pixbuf);
	
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

			[Test]
			public void AlphaLinearize ()
			{
				Linearize ("linearize.png");
			}
			
			[Test]
			public void OpaqueLinearize ()
			{
				Linearize ("linearize.jpg");
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
									     ColorCIExyY.D50,
									     ColorCIExyY.D50);

				string path = CreateFile (name, 32);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new ColorFilter (adjustment);
					Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
					req.Preserve (req.Current);
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current + "is Zero length");
					using (ImageFile img = ImageFile.Create (req.Current)) {
						Pixbuf pixbuf = img.Load ();
						Assert.IsNotNull (pixbuf);
					}
				}

			}

			public void Linearize (string name)
			{
				GammaTable table = new GammaTable (new ushort [] { 0x0000, 0x0000, 0x0000, 0x0000 });
				Profile link = new Profile (IccColorSpace.Rgb, new GammaTable [] { table, table, table });

				string path = CreateFile (name, 32);
				using (FilterRequest req = new FilterRequest (path)) {
					ColorFilter filter = new ColorFilter ();
					filter.DeviceLink = link;
					Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
					req.Preserve (req.Current);
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current + "is Zero length");
					using (ImageFile img = ImageFile.Create (req.Current)) {
						Pixbuf pixbuf = img.Load ();
						Assert.IsNotNull (pixbuf);
						// We linearized to all black so this should pass the gray test
						Assert.IsTrue (PixbufUtils.IsGray (pixbuf, 1), "failed to linearize" + req.Current);
					}
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
