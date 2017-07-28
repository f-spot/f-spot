//
// PhotoRequestHandler.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
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

using System.IO;

using FSpot.Core;
using FSpot.Filters;
using FSpot.Thumbnail;

using Hyena;

namespace FSpot.Tools.LiveWebGallery
{
	public class PhotoRequestHandler : RequestHandler
	{
		readonly LiveWebGalleryStats stats;

		public PhotoRequestHandler (LiveWebGalleryStats stats)
		{
			this.stats = stats;
		}

		public override void Handle (string requested, Stream stream)
		{
			uint photoId = uint.Parse (requested);
			Photo photo = App.Instance.Database.Photos.Get (photoId);

			SendImage (photo, stream);
					}

		protected virtual void SendImage (Photo photo, Stream stream)
		{
			string path = photo.DefaultVersion.Uri.LocalPath;
			FileInfo fileInfo = new FileInfo(path);
			if (!fileInfo.Exists) {
				SendError (stream, "404 The file is not on the disk");
				return;
			}

			FilterSet filters = new FilterSet ();
			filters.Add (new JpegFilter ());
			filters.Add (new ResizeFilter (1600));

			using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {
				filters.Convert (request);
				fileInfo = new FileInfo (request.Current.LocalPath);
				SendFile (fileInfo, photo, stream);
			}

			if (stats != null)
				stats.PhotoViews++;
		}

		protected void SendFile (FileInfo file, Photo photo, Stream dest)
		{
			stats.BytesSent += (int)file.Length;
			Log.DebugFormat ($"Sending {file.FullName}, {file.Length / 1024} kb");
			SendHeadersAndStartContent(dest, "Content-Type: " + MimeTypeForExt (file.Extension),
											 "Content-Length: " + file.Length,
											 "Last-Modified: " + photo.Time.ToString ("r"));
			using (Stream src = file.OpenRead ()) {
				byte[] buf = new byte[10240];
				int read;
				while((read = src.Read(buf, 0, buf.Length)) != 0) {
					dest.Write (buf, 0, read);
				}
			}
		}
	}

	public class ThumbnailRequestHandler : PhotoRequestHandler
	{
		public ThumbnailRequestHandler (LiveWebGalleryStats stats) : base (stats) {}

		protected override void SendImage (Photo photo, Stream dest)
		{
			Gdk.Pixbuf thumb = App.Instance.Container.Resolve<IThumbnailService> ().GetThumbnail (photo.DefaultVersion.Uri, ThumbnailSize.Large);
			byte[] buf = thumb.SaveToBuffer ("png");
			SendHeadersAndStartContent(dest, "Content-Type: " + MimeTypeForExt (".png"),
											 "Content-Length: " + buf.Length,
											 "Last-Modified: " + photo.Time.ToString ("r"));
			dest.Write (buf, 0, buf.Length);
		}
	}
}
