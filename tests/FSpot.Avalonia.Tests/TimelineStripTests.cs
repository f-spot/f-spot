// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TimelineStripTests
{
	[TestMethod]
	public void ShiftDrag_FromFirstMonthToThird_ExecutesInclusiveRange ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (4);
		(int Min, int Max)? range = null;
		var strip = new TimelineStrip {
			Width = 200,
			Height = 90,
			Items = buckets,
			RangeCommand = new RecordingRangeCommand (value => range = value)
		};
		var window = new Window {
			Width = 240,
			Height = 120,
			Content = strip
		};
		window.Show ();
		strip.UpdateLayout ();

		var months = MonthButtons (strip);
		Assert.IsGreaterThanOrEqualTo (3, months.Count);
		Point start = months[0].TranslatePoint (new Point (11, 20), window)
			?? throw new InvalidOperationException ("first month has no window point");
		Point end = months[2].TranslatePoint (new Point (11, 20), window)
			?? throw new InvalidOperationException ("third month has no window point");

		window.MouseDown (start, MouseButton.Left, RawInputModifiers.Shift);
		window.MouseMove (end, RawInputModifiers.Shift);
		window.MouseUp (end, MouseButton.Left, RawInputModifiers.Shift);

		Assert.AreEqual ((0, 2), range);
	}

	static List<TimelineBucket> Months (int count)
	{
		var months = new List<TimelineBucket> ();
		for (int i = 0; i < count; i++) {
			var start = new DateTime (2026, i + 1, 1);
			months.Add (new TimelineBucket {
				MonthLabel = start.ToString ("MMM"),
				Start = start,
				End = start.AddMonths (1),
				Count = 1,
				RelativeHeight = 1
			});
		}
		return months;
	}

	static List<Button> MonthButtons (TimelineStrip strip)
		=> strip.GetVisualDescendants ().OfType<Button> ()
			.Where (button => button.Classes.Contains ("timeline-month"))
			.ToList ();

	sealed class RecordingRangeCommand : ICommand
	{
		readonly Action<(int Min, int Max)> record;

		public RecordingRangeCommand (Action<(int Min, int Max)> record)
			=> this.record = record;

		public bool CanExecute (object? parameter) => true;

		public void Execute (object? parameter)
			=> record (((int Min, int Max))parameter!);

		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
