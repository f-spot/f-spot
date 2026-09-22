// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Platform;

namespace FSpot.Avalonia;

static class PackedIconFiles
{
	static IReadOnlyCollection<string>? files;

	public static IReadOnlyCollection<string> All
	{
		get {
			if (files is { Count: > 0 })
				return files;
			files = Discover ();
			return files;
		}
	}

	static IReadOnlyCollection<string> Discover ()
	{
		var names = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		try {
			foreach (var uri in AssetLoader.GetAssets (new Uri ("avares://FSpot.Avalonia/Icons/"), null)) {
				string name = Path.GetFileName (uri.AbsolutePath);
				if (!string.IsNullOrEmpty (name))
					names.Add (Uri.UnescapeDataString (name));
			}
		} catch (Exception) {
			// Headless tests that never started Avalonia still see an empty set.
		}
		return names;
	}
}
