//
// SharpFilter.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007 Stephane Delcroix
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

using Gdk;

using FSpot.Imaging;
using FSpot.Utils;

namespace FSpot.Filters
{
    public class SharpFilter : IFilter
    {
		readonly double threshold;
		readonly double amount;
		readonly double radius;

		public SharpFilter (double radius, double amount, double threshold)
        {
            this.radius = radius;
            this.amount = amount;
            this.threshold = threshold;
        }

		public bool Convert (FilterRequest req)
		{
			var dest_uri = req.TempUri (req.Current.GetExtension ());

			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (req.Current)) {
				using (Pixbuf in_pixbuf = img.Load ()) {
					using (Pixbuf out_pixbuf = PixbufUtils.UnsharpMask (in_pixbuf, radius, amount, threshold, null)) {
						FSpot.Utils.PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, out_pixbuf);
					}
				}
			}

			req.Current = dest_uri;
			return true;
		}
	}
}
