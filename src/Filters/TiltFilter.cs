/*
 * Filters/TiltFilter.cs
 *
 * Author(s)
 *
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */
using System;
using System.IO;
using FSpot;
using FSpot.Widgets;

using Cairo;
using Gdk;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace FSpot.Filters {
	public class TiltFilter : IFilter
	{
		double angle;
		public TiltFilter (double angle)
		{
			this.angle = angle;
		}

		public bool Convert (FilterRequest req)
		{
			string source = req.Current.LocalPath;
			Uri dest = req.TempUri (Path.GetExtension (source));
			
			using (ImageFile img = ImageFile.Create (source)) {
				using (Pixbuf pixbuf = img.Load ()) {
					using (ImageInfo info = new ImageInfo (pixbuf)) {
						MemorySurface surface = new MemorySurface (Format.Argb32, 
											   pixbuf.Width,
											   pixbuf.Height);
	
						Context ctx = new Context (surface);
						ctx.Matrix = info.Fill (info.Bounds, angle);
						Pattern p = new SurfacePattern (info.Surface);
						ctx.Source = p;
						ctx.Paint ();
						((IDisposable)ctx).Dispose ();
						p.Destroy ();
						using (Pixbuf result = CairoUtils.CreatePixbuf (surface)) {
							using (Stream output = File.OpenWrite (dest.LocalPath)) {
								img.Save (result, output);
							}
						}
						surface.Flush ();
						info.Dispose ();
						req.Current = dest;
						return true;
					}
				}
			}
		}
		
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest
		{
			[Test]
			public void TestPng ()
			{
				Basic ("file.png");
			}
			  
			[Test]
			public void TestJpeg ()
			{
				Basic ("file.jpg");
			}

			[Test]
			public void TestTiff ()
			{
				Basic ("file.tiff");
			}

			public void Basic (string name)
			{
				string path = CreateFile (name, 120);
				using (FilterRequest req = new FilterRequest (path)) {
					IFilter filter = new TiltFilter (Math.PI / 4);
					filter.Convert (req);
					Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
						       "Error: Did not create " + req.Current.LocalPath);
					Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
						       "Error: " + req.Current.LocalPath + "is Zero length");
				}
			}
		}
#endif
	}
}
