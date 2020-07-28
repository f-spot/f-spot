//
// MultiImportSource.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.FileSystem;
using FSpot.Imaging;

using Hyena;

namespace FSpot.Import
{
	public class MultiImportSource : ImportSource
	{
		readonly IEnumerable<SafeUri> uris;

		public MultiImportSource (IEnumerable<SafeUri> uris)
			: base (null, string.Empty, string.Empty)
		{
			this.uris = uris;
		}

		public override IImportSource GetFileImportSource (IImageFileFactory factory, IFileSystem fileSystem)
		{
			return new MultiFileImportSource (uris, factory, fileSystem);
		}
	}
}
