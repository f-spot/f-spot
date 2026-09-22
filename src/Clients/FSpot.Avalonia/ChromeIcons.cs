// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

public static class ChromeIcons
{
	public const string AppLogo = "avares://FSpot.Avalonia/Icons/f-spot.svg";

	// Avalonia WindowIcon is a bitmap; keep the 32px PNG for the OS window slot only.
	public const string WindowIcon = "/Icons/f-spot-32.png";

	const string AvaresPrefix = "avares://FSpot.Avalonia/Icons/";

	public static string? PreferredFile (string? iconName, IEnumerable<string> packedFiles)
	{
		if (string.IsNullOrEmpty (iconName))
			return null;
		string stem = iconName;
		if (stem.EndsWith (".svg", StringComparison.OrdinalIgnoreCase)
			|| stem.EndsWith (".png", StringComparison.OrdinalIgnoreCase))
			stem = Path.GetFileNameWithoutExtension (stem);
		var packed = packedFiles as ISet<string>
			?? new HashSet<string> (packedFiles, StringComparer.OrdinalIgnoreCase);
		foreach (string candidate in Candidates (stem)) {
			if (packed.Contains (candidate))
				return candidate;
		}
		return null;
	}

	public static string? AvaresUri (string? iconName, IEnumerable<string> packedFiles)
	{
		string? file = PreferredFile (iconName, packedFiles);
		return file is null ? null : AvaresPrefix + file;
	}

	static IEnumerable<string> Candidates (string stem)
	{
		yield return stem + ".svg";
		yield return stem + "-22.svg";
		yield return stem + "-16.svg";
		yield return stem + "-32.svg";
		yield return stem + ".png";
		yield return stem + "-22.png";
		yield return stem + "-16.png";
		yield return stem + "-32.png";
	}
}
