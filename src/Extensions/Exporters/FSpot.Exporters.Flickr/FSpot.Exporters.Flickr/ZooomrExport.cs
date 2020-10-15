//  ZooomrExport.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2015 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FlickrNet;

using FSpot.Core;

namespace FSpot.Exporters.Flickr
{
    public class ZooomrExport : FlickrExport
	{
		public override void Run (IBrowsableCollection selection)
		{
			Run (SupportedService.Zooomr, selection, false);
		}
	}
}
