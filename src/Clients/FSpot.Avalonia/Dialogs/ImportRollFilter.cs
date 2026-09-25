// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// One import roll offered by the Select Import Rolls dialog, newest first.
/// </summary>
public sealed record ImportRollOption (uint Id, DateTime TimeUtc, uint PhotoCount)
{
	public string Label => ImportRollFilter.Header (TimeUtc, PhotoCount);
}

public enum ImportRollMode
{
	At,
	After,
	Between
}

/// <summary>
/// The at / after / between rules of the GTK LastRolls dialog.
/// </summary>
public static class ImportRollFilter
{
	/// <summary>
	/// "dd MMM, HH:mm (count)" in local time; roll times are stored in UTC.
	/// </summary>
	public static string Header (DateTime timeUtc, uint photoCount)
	{
		var local = DateTime.SpecifyKind (timeUtc, DateTimeKind.Utc).ToLocalTime ();
		return local.ToString ("dd MMM, HH:mm", CultureInfo.CurrentCulture) + " (" + photoCount.ToString (CultureInfo.CurrentCulture) + ")";
	}

	/// <summary>
	/// The rolls a choice covers. Rolls are ordered newest first, so "after" means every
	/// roll from the newest down to the chosen one, and "between" is inclusive in either order.
	/// </summary>
	public static IReadOnlyList<ImportRollOption> Select (IReadOnlyList<ImportRollOption> rolls, ImportRollMode mode, int first, int second)
	{
		if (first < 0 || first >= rolls.Count)
			return [];
		switch (mode) {
		case ImportRollMode.At:
			return [rolls[first]];
		case ImportRollMode.After:
			return rolls.Take (first + 1).ToList ();
		default:
			if (second < 0 || second >= rolls.Count)
				return [];
			int low = Math.Min (first, second);
			int high = Math.Max (first, second);
			return rolls.Skip (low).Take (high - low + 1).ToList ();
		}
	}

	public static uint PhotoCount (IEnumerable<ImportRollOption> rolls)
	{
		uint sum = 0;
		foreach (var roll in rolls)
			sum += roll.PhotoCount;
		return sum;
	}
}
