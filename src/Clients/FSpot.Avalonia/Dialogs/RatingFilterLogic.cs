// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// The Set Rating Filter dialog's rules, ported from the GTK RatingFilterDialog.
/// </summary>
public static class RatingFilterLogic
{
	public const uint DefaultMin = 4;
	public const uint DefaultMax = 5;
	public const uint MaxStars = 5;

	/// <summary>
	/// The starting values: the active rating filter, or 4 to 5 stars when none is set.
	/// </summary>
	public static (uint Min, uint Max) Initial (uint? currentMin, uint? currentMax)
	{
		if (currentMin is null && currentMax is null)
			return (DefaultMin, DefaultMax);
		uint min = Math.Min (currentMin ?? 0, MaxStars);
		uint max = Math.Min (currentMax ?? MaxStars, MaxStars);
		return max < min ? (min, min) : (min, max);
	}

	/// <summary>
	/// Raising the minimum above the maximum drags the maximum up with it.
	/// </summary>
	public static (uint Min, uint Max) WithMin (uint min, uint max)
	{
		min = Math.Min (min, MaxStars);
		return (min, Math.Max (min, max));
	}

	/// <summary>
	/// Lowering the maximum below the minimum drags the minimum down with it.
	/// </summary>
	public static (uint Min, uint Max) WithMax (uint min, uint max)
	{
		max = Math.Min (max, MaxStars);
		return (Math.Min (min, max), max);
	}
}
