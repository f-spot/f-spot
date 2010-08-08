/*
 * Filters/JpegFilter.cs
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using FSpot.Utils;
using FSpot.Imaging;

namespace FSpot.Filters {
    public class JpegFilter : IFilter {
        private uint quality = 95;
        public uint Quality {
            get { return quality; }
            set { quality = value; }
        }

        public JpegFilter (uint quality)
        {
            this.quality = quality;
        }

        public JpegFilter()
        {
        }

        public bool Convert (FilterRequest req)
        {
            var source = req.Current;
            req.Current = req.TempUri ("jpg");

            PixbufUtils.CreateDerivedVersion (source, req.Current, quality);

            return true;
        }
    }
}
