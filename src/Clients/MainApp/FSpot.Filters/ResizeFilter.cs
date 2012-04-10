//
// ResizeFilter.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using FSpot.Imaging;

using Gdk;

namespace FSpot.Filters {
    public class ResizeFilter : IFilter
    {
    	public ResizeFilter (uint size)
        {
            Size = size;
        }

    	public uint Size { get; set; }

    	public bool Convert (FilterRequest req)
        {
            string source = req.Current.LocalPath;
            var dest_uri = req.TempUri (System.IO.Path.GetExtension (source));

            using (var img = ImageFile.Create (req.Current)) {

                using (Pixbuf pixbuf = img.Load ()) {
                    if (pixbuf.Width < Size && pixbuf.Height < Size)
                        return false;
                }

                using (Pixbuf pixbuf = img.Load ((int)Size, (int)Size)) {
                    PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, pixbuf);
                }
            }

            req.Current = dest_uri;
            return true;
        }
    }
}
