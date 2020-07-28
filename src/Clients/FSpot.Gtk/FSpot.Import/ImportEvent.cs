//
// ImportEvent.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Import
{
	public enum ImportEvent
	{
		SourceChanged,
		PhotoScanStarted,
		PhotoScanFinished,
		ImportStarted,
		ImportFinished,
		ImportError
	}
}
