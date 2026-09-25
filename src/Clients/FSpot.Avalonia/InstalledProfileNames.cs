//
// InstalledProfileNames.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Cms;

namespace FSpot.Avalonia;

static class InstalledProfileNames
{
	static ProfileCatalog? catalog;

	static ProfileCatalog Catalog => catalog ??= ProfileCatalog.Build (built => built.AddInstalled ());

	// The profile the preferences stored under name, owned by the shared catalog.
	public static Profile? Find (string? name)
		=> !string.IsNullOrEmpty (name) && Catalog.Profiles.TryGetValue (name, out var profile) ? profile : null;

	public static IReadOnlyList<string> For (IccProfileClass deviceClass)
	{
		return Catalog.Profiles
			.Where (pair => pair.Value.DeviceClass == deviceClass)
			.Select (pair => pair.Key)
			.OrderBy (name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray ();
	}
}
