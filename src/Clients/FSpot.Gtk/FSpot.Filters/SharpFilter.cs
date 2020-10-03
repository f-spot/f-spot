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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
				using Pixbuf in_pixbuf = img.Load ();
				using Pixbuf out_pixbuf = PixbufUtils.UnsharpMask (in_pixbuf, radius, amount, threshold, null);
				FSpot.Utils.PixbufUtils.CreateDerivedVersion (req.Current, dest_uri, 95, out_pixbuf);
			}

			req.Current = dest_uri;
			return true;
		}
	}
}
