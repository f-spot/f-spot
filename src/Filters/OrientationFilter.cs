/*
 * OrientationFilter.cs
 *
 * Author(s)
 *   Larry Ewing
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software, see COPYING fro details
 *
 */

using FSpot.Utils;

namespace FSpot.Filters {
	public class OrientationFilter : IFilter {
		public bool Convert (FilterRequest req)
		{
			string source = req.Current.LocalPath;
			var dest_uri = req.TempUri (System.IO.Path.GetExtension (source));
			string dest = dest_uri.LocalPath;

			using (ImageFile img = ImageFile.Create (req.Current)) {
				bool changed = false;
				
				if (img.Orientation != PixbufOrientation.TopLeft && img is JpegFile) {
					JpegFile jimg = img as JpegFile;
					
					if (img.Orientation == PixbufOrientation.RightTop) {
						JpegUtils.Transform (source,
								     dest,
								     JpegUtils.TransformType.Rotate90);
						changed = true;
					} else if (img.Orientation == PixbufOrientation.LeftBottom) {
						JpegUtils.Transform (source,
								     dest,
								     JpegUtils.TransformType.Rotate270);
						changed = true;
					} else if (img.Orientation == PixbufOrientation.BottomRight) {
						JpegUtils.Transform (source,
								     dest,
								     JpegUtils.TransformType.Rotate180);
						changed = true;
					}
					
					int width, height;
	
					using (jimg = ImageFile.Create (dest_uri) as JpegFile) {
						PixbufUtils.GetSize (dest, out width, out height);

						jimg.SetOrientation (PixbufOrientation.TopLeft);
						jimg.SetDimensions (width, height);

						using (Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (dest, 160, 120, true)) {
							jimg.SetThumbnail (pixbuf);
						}

						jimg.SaveMetaData (dest);
						jimg.Dispose ();
					}
				}
	
				if (changed)
					req.Current = dest_uri;
	
				return changed;
			}
		}
		
	}
}
