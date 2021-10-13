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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Utils;

using Hyena;

namespace FSpot.Import
{
	public class ImportSource
	{
		public string Name { get; }

		public string IconName { get; }

		public SafeUri Root { get; }

		public ImportSource (SafeUri root, string name, string iconName)
		{
			Name = name;
			Root = root;

			if (root == null)
				return;

			if (IsIPodPhoto) {
				IconName = "multimedia-player";
			} else if (IsCamera) {
				IconName = "media-flash";
			} else {
				IconName = iconName;
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
