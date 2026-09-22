// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public static class PromptCopy
{
	public static string DateRange => Strings.DateRangePrompt;
	public static string RollId => Strings.RollIdPrompt;

	public static string RollHint (IReadOnlyList<(uint Id, string Label)> rolls)
		=> rolls.Count == 0
			? RollId
			: string.Join (", ", rolls.Select (roll => $"{roll.Id}:{roll.Label}"));
}
