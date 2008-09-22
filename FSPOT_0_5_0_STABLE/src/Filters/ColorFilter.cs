/*
 * FSpot.Filters.ColorFilter.cs
 *
 * Author(s)
 *
 *   Larry Ewing <lewing@novell.com>
 *   Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using System.IO;
using Gdk;
using Cms;

using FSpot.Utils;
using FSpot.ColorAdjustment;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot.Filters {
	public abstract class ColorFilter : IFilter {
		public bool Convert (FilterRequest req)
		{
			Uri source = req.Current;
			using (ImageFile img = ImageFile.Create (source)) {
				Pixbuf pixbuf = img.Load ();
				Profile profile = img.GetProfile ();

				Adjustment adjustment = CreateAdjustment (pixbuf, profile);
				Gdk.Pixbuf final = adjustment.Adjust ();

				Uri dest_uri = req.TempUri (Path.GetExtension (source.LocalPath));
				using (Stream output = File.OpenWrite (dest_uri.LocalPath)) {
					img.Save (final, output);
				}
				final.Dispose ();
				req.Current = dest_uri;
				
				return true;
			}
		}

		protected abstract Adjustment CreateAdjustment (Pixbuf input, Cms.Profile input_profile);
	}

	public class AutoStretchFilter : ColorFilter {
		protected override Adjustment CreateAdjustment (Pixbuf input, Cms.Profile input_profile) {
			return new FSpot.ColorAdjustment.AutoStretch (input, input_profile);
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
				set.Add (new AutoStretchFilter ());
				set.Add (new UniqueNameFilter (Path.GetDirectoryName (path)));
				set.Convert (req);
				req.Preserve (req.Current);
			}
#endif

			public void Process (string name)
			{
				string path = CreateFile (name, 120);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new AutoStretchFilter ();
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
}
