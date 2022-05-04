//
// IDb.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Database
{
	public interface IDb
	{
		FSpotDatabaseConnection Database { get; }
		bool Sync { set; }

		TagStore Tags { get; }
		RollStore Rolls { get; }
		ExportStore Exports { get; }
		JobStore Jobs { get; }
		PhotoStore Photos { get; }
		MetaStore Meta { get; }
	}
}
