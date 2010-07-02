/*
 * Filters/SharpFilter.cs : Apply an UnsharpMask to images
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using System.IO;
using Gdk;

using Mono.Unix;

using FSpot.Utils;
using FSpot.Imaging;

namespace FSpot.Filters {
    public class SharpFilter : IFilter
    {
        double radius, amount, threshold;

        public SharpFilter (double radius, double amount, double threshold)
        {
            this.radius = radius;
            this.amount = amount;
            this.threshold = threshold;
        }

        public bool Convert (FilterRequest req)
        {
            var dest_uri = req.TempUri (req.Current.GetExtension ());

            using (var img = ImageFile.Create (req.Current)) {
                using (Pixbuf in_pixbuf = img.Load ()) {
                    using (Pixbuf out_pixbuf = PixbufUtils.UnsharpMask (in_pixbuf, radius, amount, threshold)) {
                        PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, out_pixbuf);
                    }
                }
            }

            req.Current = dest_uri;
            return true;
        }
    }
}
