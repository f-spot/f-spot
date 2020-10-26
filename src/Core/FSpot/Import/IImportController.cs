//
// IImportController.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using FSpot.Core;
using FSpot.Database;

using Hyena;

namespace FSpot.Import
{
	public interface IImportController
	{
		void DoImport (IDb db, IBrowsableCollection photos, IList<Tag> tagsToAttach, ImportPreferences preferences, IProgress<int> progress, CancellationToken token);
		int PhotosImported { get; }
		IReadOnlyList<SafeUri> FailedImports { get; }
	}
}
