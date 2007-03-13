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

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

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
			// FIXME this should copy metadata from the original
			// even when the source is not a jpeg
			string source = req.Current.LocalPath;

			using (ImageFile img = ImageFile.Create (source)) {
				if (img is JpegFile)
					return false;

				req.Current = req.TempUri ("jpg");
				string dest = req.Current.LocalPath;

				Exif.ExifData exif_data;
				try {
					exif_data = new Exif.ExifData (source);
				} catch (Exception) {
					exif_data = new Exif.ExifData();
				}

				PixbufUtils.SaveJpeg (img.Load(), dest, (int) quality, exif_data);
			}

			return true;
		}
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest
		{
			string [] Names = {
				"fspot-jpegfilter-test.png",
				"fspot-jpegfilter-test.jpeg",
				"fspot-jpegfilter-test.jpg",
				"fspot-jpegfilter-test.JPG",
			};

			[Test]
			public void ResultIsJpeg ()
			{
				foreach (string name in Names)
					ResultIsJpeg (name);
			}

			public void ResultIsJpeg (string name)
			{
				string path = CreateFile (name, 48);
				IFilter filter = new JpegFilter ();
				FilterRequest req = new FilterRequest (path);
				filter.Convert (req);
				using (ImageFile img = new JpegFile (req.Current)) {
					Assert.IsTrue (img != null, "result is null");
					Assert.IsTrue (img is JpegFile, "result is not a jpg");
				}
				System.IO.File.Delete (path);
			}

			[Test]
			public void ExtensionIsJPG ()
			{
				foreach (string name in Names)
					ExtensionIsJPG (name);
			}

			public void ExtensionIsJPG (string name)
			{
				string path = CreateFile (name, 48);
				IFilter filter = new JpegFilter ();
				FilterRequest req = new FilterRequest (path);
				filter.Convert (req);
				string extension = System.IO.Path.GetExtension (req.Current.LocalPath).ToLower ();
				Assert.IsTrue (extension == ".jpg" || extension == ".jpeg", String.Format ("{0} is not a valid extension for Jpeg", extension));
				System.IO.File.Delete (path);
			}

			[Test]
			public void OriginalUntouched ()
			{
				foreach (string name in Names)
					OriginalUntouched (name);
			}

			public void OriginalUntouched (string name)
			{
				string path = CreateFile (name, 48);
				IFilter filter = new JpegFilter ();
				FilterRequest req = new FilterRequest (path);
				long original_size = new System.IO.FileInfo (path).Length;
				filter.Convert (req);
				long final_size = new System.IO.FileInfo (req.Source.LocalPath).Length;
				Assert.IsTrue (original_size == final_size, "original is modified !!!");
				System.IO.File.Delete (path);
			}
		}
#endif
	}
}
