//
// TimelineBucket.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Query;

namespace FSpot.Avalonia.ViewModels;

public partial class TimelineBucket : ObservableObject
{
	[ObservableProperty]
	bool selected;

	[ObservableProperty]
	bool inRange;

	public required string MonthLabel { get; init; }
	public string? YearLabel { get; init; }
	public required DateTime Start { get; init; }
	public required DateTime End { get; init; }
	public required int Count { get; init; }
	public required double RelativeHeight { get; init; }
	public double BarHeight => Count == 0 ? 0 : Math.Max (3, RelativeHeight * 40);
	public bool HasPhotos => Count > 0;
	public string ToolTip => QueryChipFormat.TimelineMonth (Start, Count);
	public string AutomationId => $"timeline.{Start:yyyy-MM}";
}
