/*
 * FSpot.ThumbnailGenerator.cs
 *
 * Author(s)
 * 	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Hyena;
using FSpot.Utils;
using FSpot.Platform;

using Mono.Unix.Native;
using GFileInfo = GLib.FileInfo;

namespace FSpot {
	public class ThumbnailGenerator : ImageLoaderThread {

		static public ThumbnailGenerator Default = new ThumbnailGenerator ();

		public static Gdk.Pixbuf Create (SafeUri uri)
		{
			try {
				Gdk.Pixbuf thumb;

				using (ImageFile img = ImageFile.Create (uri)) {
					thumb = img.Load (256, 256);
				}

				if (thumb == null)
					return null;

				Save (thumb, uri);
				return thumb;
			} catch (Exception e) {
				Log.Exception (e);
				return null;
			}
		}
		
		private static void Save (Gdk.Pixbuf image, SafeUri uri)
		{
			try {
				ThumbnailCache.Default.RemoveThumbnailForUri (uri);
			} finally {
				ThumbnailFactory.SaveThumbnail (image, uri);
			}
		}

		protected override void EmitLoaded (System.Collections.Queue results)
		{
			base.EmitLoaded (results);
			
			foreach (RequestItem r in results) {
				if (r.result != null)
					r.result.Dispose ();
			}
				
		}

		protected override void ProcessRequest (RequestItem request)
		{
			try {
				base.ProcessRequest (request);

				Gdk.Pixbuf image = request.result;
				if (image != null)
					Save (image, request.uri);

				System.Threading.Thread.Sleep (75);
			} catch (System.Exception e) {
				Log.Exception (e);
			}
		}

	}
}
