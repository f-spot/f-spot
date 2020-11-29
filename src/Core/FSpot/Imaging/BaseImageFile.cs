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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

		public SafeUri Uri { get; }

		public ImageOrientation Orientation { get; private set; }

		public BaseImageFile (SafeUri uri)
		{
			Uri = uri;
			Orientation = ImageOrientation.TopLeft;

			using var metadataFile = MetadataUtils.Parse (uri);
			ExtractMetadata (metadataFile);
		}

		protected virtual void ExtractMetadata (TagLib.Image.File metadata)
		{
			if (metadata != null)
				Orientation = metadata.ImageTag.Orientation;
		}

		public virtual Stream PixbufStream ()
		{
			Log.Debug ($"open uri = {Uri}");
			return new FileStream (Uri.AbsolutePath, FileMode.Open, FileAccess.Read);
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
			using Stream stream = PixbufStream ();

			var orig = new Gdk.Pixbuf (stream);
			return TransformAndDispose (orig);
		}

		public Gdk.Pixbuf Load (int maxWidth, int maxHeight)
		{
			using var full = Load ();

			return full.ScaleToMaxSize (maxWidth, maxHeight);
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
