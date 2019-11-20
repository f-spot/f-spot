//
// ThumbnailerFactory.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
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

using FSpot.FileSystem;
using FSpot.Imaging;
using Hyena;

namespace FSpot.Thumbnail
{
	class ThumbnailerFactory : IThumbnailerFactory
	{
		readonly IImageFileFactory factory;
		readonly IFileSystem fileSystem;

		public ThumbnailerFactory(IImageFileFactory factory, IFileSystem fileSystem)
		{
			this.factory = factory;
			this.fileSystem = fileSystem;
		}

		#region IThumbnailerFactory implementation

		public IThumbnailer GetThumbnailerForUri (SafeUri uri)
		{
			if (factory.HasLoader (uri)) {
				return new ImageThumbnailer (uri, factory, fileSystem);
			}
			return null;
		}

		#endregion
	}
}
