//
// ImportSource.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2019 Stephen Shaw
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

using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Utils;

using Hyena;

namespace FSpot.Import
{
	public class ImportSource
	{
		public string Name { get; private set; }

		public string IconName { get; private set; }

		public SafeUri Root { get; private set; }

		public ImportSource (SafeUri root, string name, string iconName)
		{
			Name = name;
			Root = root;

			if (root != null) {
				if (IsIPodPhoto) {
					IconName = "multimedia-player";
				} else if (IsCamera) {
					IconName = "media-flash";
				} else {
					IconName = iconName;
				}
			}
		}

		public virtual IImportSource GetFileImportSource (IImageFileFactory factory, IFileSystem fileSystem)
		{
			return new FileImportSource (Root, factory, fileSystem);
		}

		bool IsCamera {
			get => File.Exists (Root.Append ("DCIM").AbsolutePath);
		}

		bool IsIPodPhoto {
			get {
				var file = File.Exists (Root.Append ("Photos").AbsolutePath);
				var file2 = File.Exists (Root.Append ("iPod_Control").AbsolutePath);
				return file && file2;
			}
		}
	}
}
