/*
 * Filters/ResizeFilter.cs
 *
 * Author(s)
 *
 *   Stephane Delcroix <stephane@delcroix.org>
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using System.IO;

using FSpot.Utils;
using FSpot.Imaging;

using Mono.Unix;

using Gdk;

namespace FSpot.Filters {
    public class ResizeFilter : IFilter
    {
        public ResizeFilter ()
        {
        }

        public ResizeFilter (uint size)
        {
            this.size = size;
        }

        private uint size = 600;

        public uint Size {
            get { return size; }
            set { size = value; }
        }

        public bool Convert (FilterRequest req)
        {
            string source = req.Current.LocalPath;
            var dest_uri = req.TempUri (System.IO.Path.GetExtension (source));

            using (var img = ImageFile.Create (req.Current)) {

                using (Pixbuf pixbuf = img.Load ()) {
                    if (pixbuf.Width < size && pixbuf.Height < size)
                        return false;
                }

                using (Pixbuf pixbuf = img.Load ((int)size, (int)size)) {
                    PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, pixbuf);
                }
            }

            req.Current = dest_uri;
            return true;
        }
    }
}
