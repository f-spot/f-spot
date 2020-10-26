//
// UriExtensions.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2012 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace FSpot.Utils
{
	public static class TargetListExtensionMethods
	{
		public static void AddTargetEntry (this TargetList targetList, TargetEntry entry)
		{
			targetList.Add (entry.Target, (uint)entry.Flags, entry.Info);
		}
	}
}

