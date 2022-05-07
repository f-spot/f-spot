// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Database
{
	public interface IDb
	{
		FSpotContext Context { get; }

		TagStore Tags { get; }
		RollStore Rolls { get; }
		ExportStore Exports { get; }
		JobStore Jobs { get; }
		PhotoStore Photos { get; }
		MetaStore Meta { get; }
	}
}
