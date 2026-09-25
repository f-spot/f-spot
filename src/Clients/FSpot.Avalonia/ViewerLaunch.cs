// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

// f-spot --view FILE...: the GTK Startup opened a SingleView over the named files
// instead of the organizer. Null when the organizer should start. GTK refused --view
// with no files; here that falls back to the organizer rather than an empty viewer.
public static class ViewerLaunch
{
	public static IReadOnlyList<string>? Paths (string[]? args)
	{
		if (args is null || args.Length == 0)
			return null;
		CommandLineOptions options;
		try {
			options = CommandLineOptions.Parse (args);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Cannot parse the command line");
			return null;
		}
		if (!options.View)
			return null;
		if (options.Files.Count == 0) {
			Logger.Log.Error ("f-spot: -view option takes at least one argument");
			return null;
		}
		return options.Files;
	}
}
