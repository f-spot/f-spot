//
// IImportSource.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2014 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Import
{
	public interface IImportSource
	{
		IEnumerable<FileImportInfo> ScanPhotos (ImportPreferences preferences);
	}
}
