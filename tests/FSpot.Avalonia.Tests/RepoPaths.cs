// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Tests;

static class RepoPaths
{
	public static string Root ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return dir.FullName;
		}

		throw new InvalidOperationException ("F-Spot.slnx was not found above the test output.");
	}
}
