//
// JpegFilter.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Filters
{
	public class JpegFilter : IFilter
	{
		public uint Quality { get; set; } = 95;

		public JpegFilter ()
		{
		}

		public JpegFilter (uint quality)
		{
			Quality = quality;
		}

		public bool Convert (FilterRequest req)
		{
			var source = req.Current;
			req.Current = req.TempUri ("jpg");

			PixbufUtils.CreateDerivedVersion (source, req.Current, Quality);

			return true;
		}
	}
}
