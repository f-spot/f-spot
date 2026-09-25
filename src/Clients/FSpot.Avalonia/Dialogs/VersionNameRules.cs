// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

public enum VersionNameRequest
{
	Create,
	Rename
}

/// <summary>
/// The GTK VersionNameDialog's rules.
/// </summary>
public static class VersionNameRules
{
	public static string Title (VersionNameRequest request)
		=> request == VersionNameRequest.Create ? Strings.CreateNewVersion : Strings.RenameVersion;

	public static string Prompt (VersionNameRequest request)
		=> request == VersionNameRequest.Create ? Strings.NameColon : Strings.NewNameColon;

	/// <summary>
	/// A name another version already uses. Renaming may keep the current name.
	/// </summary>
	public static bool InUse (VersionNameRequest request, string name, string currentName, IEnumerable<string> existingNames)
	{
		if (request == VersionNameRequest.Rename && name == currentName)
			return false;
		return existingNames.Contains (name, StringComparer.Ordinal);
	}

	public static bool CanAccept (VersionNameRequest request, string? name, string currentName, IEnumerable<string> existingNames)
		=> !string.IsNullOrEmpty (name) && !InUse (request, name, currentName, existingNames);

	/// <summary>
	/// The name the dialog hands back: null for cancel, and for a rename that changed nothing.
	/// </summary>
	public static string? Result (VersionNameRequest request, string? name, string currentName)
	{
		if (string.IsNullOrEmpty (name))
			return null;
		if (request == VersionNameRequest.Rename && name == currentName)
			return null;
		return name;
	}
}
