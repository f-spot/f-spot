//
// ImageFileFactory.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2016 Daniel Köb
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
using System.Collections.Generic;
using System.Linq;
using FSpot.FileSystem;
using FSpot.Utils;
using Gdk;
using Hyena;

namespace FSpot.Imaging
{
	class ImageFileFactory : IImageFileFactory
	{
		#region fields

		readonly TinyIoCContainer container;
		readonly List<string> imageTypes;
		readonly List<string> jpegExtensions;
		readonly List<string> rawExtensions;

		readonly IFileSystem fileSystem;

		#endregion

		#region ctors

		public ImageFileFactory (IFileSystem fileSystem)
		{
			container = new TinyIoCContainer ();
			imageTypes = new List<string> ();
			jpegExtensions = new List<string> ();
			rawExtensions = new List<string> ();

			this.fileSystem = fileSystem;

			RegisterTypes ();
		}

		#endregion

		#region private implementation

		enum ImageType {
			Other,
			Jpeg,
			Raw
		}

		void RegisterTypes ()
		{
			// Plain image file extenions
			RegisterExtensions<BaseImageFile> (ImageType.Other,
				".gif",
				".pcx",
				".pnm",
				".pbm",
				".pgm",
				".ppm",
				".bmp",
				".png",
				".tif", ".tiff",
				".svg", ".svgz");

			// Jpegs
			RegisterExtensions<BaseImageFile> (ImageType.Jpeg,
				".jfi", ".jfif", ".jif", ".jpe", ".jpeg", ".jpg");

			// Plain image mime types
			RegisterMimeTypes<BaseImageFile> (
				"image/gif",
				"image/x-pcx",
				"image/x-portable-anymap",
				"image/x-portable-bitmap",
				"image/x-portable-graymap",
				"image/x-portable-pixmap",
				"image/x-bmp", "image/x-MS-bmp",
				"image/jpeg",
				"image/png",
				"image/tiff",
				"image/svg+xml");

			// RAW files
			RegisterExtensions<NefImageFile> (ImageType.Raw,
				".arw",
				".nef",
				".pef",
				".raw",
				".orf",
				".kdc",
				".srf");
			RegisterMimeTypes<NefImageFile> (
				"image/arw", "image/x-sony-arw",
				"image/nef", "image/x-nikon-nef",
				"image/pef", "image/x-pentax-pef",
				"image/raw", "image/x-panasonic-raw",
				"image/x-orf");

			RegisterExtensions<Cr2ImageFile> (ImageType.Raw,
				".cr2");
			RegisterMimeTypes<Cr2ImageFile> (
				"image/cr2", "image/x-canon-cr2");

			RegisterExtensions<DngImageFile> (ImageType.Raw,
				".dng");
			RegisterMimeTypes<DngImageFile> (
				"image/dng", "image/x-adobe-dng");

			RegisterExtensions<DCRawImageFile> (ImageType.Raw,
				".rw2",
				".mrw",
				".x3f",
				".srw");
			RegisterMimeTypes<DCRawImageFile> (
				"image/rw2", "image/x-raw",
				"image/x-mrw",
				"image/x-x3f");

			RegisterExtensions<CiffImageFile> (ImageType.Raw,
				".crw");
			RegisterMimeTypes<CiffImageFile> (
				"image/x-ciff");

			RegisterExtensions<RafImageFile> (ImageType.Raw,
				".raf");
			RegisterMimeTypes<RafImageFile> (
				"image/x-raf");

			// as xcf pixbufloader is not part of gdk-pixbuf, check if it's there,
			// and enable it if needed.
			foreach (PixbufFormat format in Pixbuf.Formats) {
				if (format.Name == "xcf") {
					if (format.IsDisabled)
						format.SetDisabled (false);
					RegisterExtensions<BaseImageFile> (ImageType.Other, ".xcf");
				}
			}
		}

		void RegisterMimeTypes<T> (params string[] mimeTypes)
			where T : class, IImageFile
		{
			foreach (var mimeType in mimeTypes) {
				container.Register<IImageFile, T> (mimeType).AsMultiInstance ();
		}
			imageTypes.AddRange (mimeTypes);
		}

		void RegisterExtensions<T> (ImageType type, params string[] extensions)
			where T : class, IImageFile
		{
			foreach (var extension in extensions) {
				container.Register<IImageFile, T> (extension).AsMultiInstance ();
				switch (type) {
				case ImageType.Jpeg:
					jpegExtensions.Add (extension);
					break;
				case ImageType.Raw:
					rawExtensions.Add (extension);
					break;
				}
			}
			imageTypes.AddRange (extensions);
		}

		public List<string> UnitTestImageFileTypes ()
		{
			return imageTypes;
		}

		public bool HasLoader (SafeUri uri)
		{
			return GetLoaderType (uri) != null;
		}

		string GetLoaderType (SafeUri uri)
		{
			// check if GIO can find the file, which is not the case
			// with filenames with invalid encoding
			if (!fileSystem.File.Exists (uri))
				return null;

			string extension = uri.GetExtension ().ToLower ();

			// Ignore video thumbnails
			if (extension == ".thm")
				return null;

			// Ignore empty files
			if (fileSystem.File.GetSize (uri) == 0)
				return null;

			var param = UriAsParameter (uri);

			// Get loader by mime-type
			string mime = fileSystem.File.GetMimeType (uri);
			if (container.CanResolve<IImageFile> (mime, param))
				return mime;

			// Get loader by extension
			return container.CanResolve<IImageFile> (extension, param) ? extension : null;
		}

		static NamedParameterOverloads UriAsParameter (SafeUri uri) {
			return new NamedParameterOverloads (new Dictionary<string, object> { {
					"uri",
					uri
				}
			});
		}

		#endregion

		#region IImageFileFactory implementation

		public IImageFile Create (SafeUri uri)
		{
			var name = GetLoaderType (uri);
			if (name == null)
				throw new Exception (string.Format ("Unsupported image: {0}", uri));

			try {
				return container.Resolve<IImageFile> (name, UriAsParameter (uri));
			} catch (Exception e) {
				Log.DebugException (e);
				throw e;
			}
		}

		public bool IsRaw (SafeUri uri)
		{
			var extension = uri.GetExtension ().ToLower ();
			return rawExtensions.Any (x => x == extension);
		}

		public bool IsJpeg (SafeUri uri)
		{
			var extension = uri.GetExtension ().ToLower ();
			return jpegExtensions.Any (x => x == extension);
		}

		public bool IsJpegRawPair(SafeUri file1, SafeUri file2)
		{
			return file1.GetBaseUri ().ToString () == file2.GetBaseUri ().ToString () &&
				file1.GetFilenameWithoutExtension () == file2.GetFilenameWithoutExtension () &&
				((IsJpeg (file1) && IsRaw (file2)) ||
					(IsRaw (file1) && IsJpeg (file2)));
		}

		#endregion
	}
}
