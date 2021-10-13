// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Models;

using Catalog = Mono.Unix.Catalog;

namespace FSpot
{
	public static class Constants
	{
		public static readonly Tag RootCategory =
			new Tag (null, Guid.Empty, Catalog.GetString ("(None)"));

		public static readonly Guid FavoriteTagGuid = new Guid ("920CAAD8-6480-4D8C-9E0A-468C94E777C1");
		public static readonly Guid HiddenTagGuid = new Guid ("8D8C0F4D-2FE4-4627-AC9A-7C273004B3B5");
		public static readonly Guid PeopleTagGuid = new Guid ("D67B6807-F635-4E6B-A98A-8A9F18966647");
		public static readonly Guid PlacesTagGuid = new Guid ("83128847-FD1E-4A5B-9294-3977A031F4F1");
		public static readonly Guid EventsTagGuid = new Guid ("DA4B6643-50C4-4721-908D-ED2FE09FEADE");

		public static readonly Guid MetaVersionGuid = new Guid ("9FBB58AB-1B6E-4F0A-A05E-DADD88228889");
		public static readonly Guid Meta2 = new Guid ("4B7F93EF-A736-4BC6-B6C8-1717EAAAEE94");
		public static readonly Guid MetaHiddenTagGuid = new Guid ("D1094B91-F73E-4A04-832F-D6952D11E314");
	}
}
