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
	}
}
