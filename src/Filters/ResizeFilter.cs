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

using FSpot;

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
			System.Uri dest_uri = req.TempUri (System.IO.Path.GetExtension (source));
			string dest = dest_uri.LocalPath;

			using (ImageFile img = ImageFile.Create (req.Current)) {

				using (Pixbuf pixbuf = img.Load ()) {
					if (pixbuf.Width < size && pixbuf.Height < size)
						return false;
				}
	
				using (Pixbuf pixbuf = img.Load ((int)size, (int)size)) {
					string destination_extension = Path.GetExtension (dest);
	
					if (Path.GetExtension (source).ToLower () == Path.GetExtension (dest).ToLower ()) {
						using (Stream output = File.OpenWrite (dest)) {
							img.Save (pixbuf, output);
						}
					} else if (destination_extension == ".jpg") {
						// FIXME this is a bit of a nasty hack to work around
						// the lack of being able to change the path in this filter
						// and the lack of proper metadata copying yuck
						Exif.ExifData exif_data;
	
						exif_data = new Exif.ExifData (source);
						
						PixbufUtils.SaveJpeg (pixbuf, dest, 95, exif_data);
					} else 
						throw new NotImplementedException (String.Format (Catalog.GetString ("No way to save files of type \"{0}\""), destination_extension));
				}
			}

			req.Current = dest_uri;
			return true;
		}
	}
}

