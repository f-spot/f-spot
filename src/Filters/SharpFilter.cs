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
			Uri dest_uri = req.TempUri (System.IO.Path.GetExtension (req.Current.LocalPath));

			using (ImageFile img = ImageFile.Create (req.Current)) {
				using (Pixbuf in_pixbuf = img.Load ()) {
					using (Pixbuf out_pixbuf = PixbufUtils.UnsharpMask (in_pixbuf, radius, amount, threshold)) {
						string destination_extension = Path.GetExtension (dest_uri.LocalPath);
		
						if (Path.GetExtension (req.Current.LocalPath).ToLower () == Path.GetExtension (dest_uri.LocalPath).ToLower ()) {
							using (Stream output = File.OpenWrite (dest_uri.LocalPath)) {
								img.Save (out_pixbuf, output);
							}
						} else if (destination_extension == ".jpg") {
							// FIXME this is a bit of a nasty hack to work around
							// the lack of being able to change the path in this filter
							// and the lack of proper metadata copying yuck
							Exif.ExifData exif_data;
		
							exif_data = new Exif.ExifData (req.Current.LocalPath);
							
							PixbufUtils.SaveJpeg (out_pixbuf, dest_uri.LocalPath, 90, exif_data);
						} else 
							throw new NotImplementedException (String.Format (Catalog.GetString ("No way to save files of type \"{0}\""), destination_extension));
						
					}
				}
			}

			req.Current = dest_uri;
			return true;
		}

	}
}

