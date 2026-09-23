//
// InstalledProfileNames.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Cms;

namespace FSpot.Avalonia;

static class InstalledProfileNames
{
	static ProfileCatalog? catalog;

	public static IReadOnlyList<string> For (IccProfileClass deviceClass)
	{
		catalog ??= ProfileCatalog.Build (built => built.AddInstalled ());
		return catalog.Profiles
			.Where (pair => pair.Value.DeviceClass == deviceClass)
			.Select (pair => pair.Key)
			.OrderBy (name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray ();
	}
}
