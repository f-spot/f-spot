//
// TagNames.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Models;

public static class TagNames
{
	/// <summary>
	/// GTK <c>TagSelectionWidget.HandleTagNameEdited</c>: a new name that only changes the case
	/// of the old one is fine; any other name already in the store is taken.
	/// </summary>
	public static bool IsTakenByAnother (string oldName, string newName, Func<string, bool> exists)
	{
		if (string.IsNullOrWhiteSpace (newName) || newName == oldName)
			return false;
		if (string.Compare (newName, oldName, StringComparison.OrdinalIgnoreCase) == 0)
			return false;
		return exists (newName);
	}
}
