//
//  FileImportInfo.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

using Hyena;

namespace FSpot.Import
{
	public class FileImportInfo : FilePhoto
	{
		public FileImportInfo (SafeUri original, string name) : base (original, name)
		{
		}

		public SafeUri DestinationUri { get; set; }

		internal uint PhotoId { get; set; }
	}
}
