//
// BaseImageFile.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
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

using System;
using System.IO;
using FSpot.Utils;
using Hyena;
using TagLib.Image;

namespace FSpot.Imaging
{
	class BaseImageFile : IImageFile
	{
		bool disposed;

		public SafeUri Uri { get; private set; }

		public ImageOrientation Orientation { get; private set; }

		public BaseImageFile (SafeUri uri)
		{
			Uri = uri;
			Orientation = ImageOrientation.TopLeft;

			using (var metadata_file = Metadata.Parse (uri)) {
				ExtractMetadata (metadata_file);
			}
		}

		protected virtual void ExtractMetadata (TagLib.Image.File metadata)
		{
			if (metadata != null)
				Orientation = metadata.ImageTag.Orientation;
		}

		public virtual Stream PixbufStream ()
		{
			Log.DebugFormat ("open uri = {0}", Uri);
			return new GLib.GioStream (GLib.FileFactory.NewForUri (Uri).Read (null));
		}

		protected Gdk.Pixbuf TransformAndDispose (Gdk.Pixbuf orig)
		{
			if (orig == null)
				return null;

			Gdk.Pixbuf rotated = orig.TransformOrientation (Orientation);

			orig.Dispose ();

			return rotated;
		}

		public Gdk.Pixbuf Load ()
		{
			using (Stream stream = PixbufStream ()) {
				var orig = new Gdk.Pixbuf (stream);
				return TransformAndDispose (orig);
			}
		}

		public Gdk.Pixbuf Load (int maxWidth, int maxHeight)
		{
			using (var full = Load ()) {
				return full.ScaleToMaxSize (maxWidth, maxHeight);
			}
		}

		// FIXME this need to have an intent just like the loading stuff.
		public virtual Cms.Profile GetProfile ()
		{
			return null;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
				Close ();
		}

		protected virtual void Close ()
		{
		}
	}
}
