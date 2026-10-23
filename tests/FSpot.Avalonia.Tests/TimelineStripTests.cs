// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.ViewModels;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TimelineStripTests
{
	readonly List<Window> windows = [];

	[UiTestMethod]
	public void DarkTheme_YearInk_IsLightOnTheGrayChrome ()
	{
		AvaloniaApp.Ensure ();
		var scale = ShownScale (ThemeVariant.Dark);

		var ink = ThemeInk.Foreground (scale);
		var chrome = ThemeInk.Chrome (scale);
		Assert.IsGreaterThan (0.62, Luminance (ink), ink.ToString ());
		Assert.IsLessThan (0.55, Luminance (chrome), chrome.ToString ());
	}

	[UiTestMethod]
	public void LightTheme_YearInk_IsDarkOnTheChrome ()
	{
		AvaloniaApp.Ensure ();
		var scale = ShownScale (ThemeVariant.Light);

		var ink = ThemeInk.Foreground (scale);
		Assert.IsLessThan (0.35, Luminance (ink), ink.ToString ());
		Assert.AreEqual (255, ink.A);
	}

	[UiTestMethod]
	public void LightTheme_YearInk_StaysDarkWhenBaseHighIsFaint ()
	{
		AvaloniaApp.Ensure ();
		var scale = ShownScale (ThemeVariant.Light);
		// Fluent's light base-high is black. A build that hands back a light gray, or a
		// translucent black that paints as gray, must not become the year caption.
		scale.Resources["SystemBaseHighColor"] = Color.Parse ("#FFD6D6D6");
		var faint = ThemeInk.Foreground (scale);
		Assert.IsLessThan (0.35, Luminance (faint), faint.ToString ());

		scale.Resources["SystemBaseHighColor"] = Color.FromArgb (0x55, 0, 0, 0);
		var washed = ThemeInk.Foreground (scale);
		Assert.IsLessThan (0.35, Luminance (washed), washed.ToString ());
		Assert.AreEqual (255, washed.A);
	}

	[UiTestMethod]
	public void LightTrack_YearInk_StaysDarkWhenTheVariantWouldAcceptBaseHigh ()
	{
		AvaloniaApp.Ensure ();
		// Not Light and not Dark, so a luminance check that only special-cases those two
		// keeps the faint base-high color. The track is still the light chrome.
		var scale = ShownScale (new ThemeVariant ("Paper", null));
		scale.Resources["SystemBaseHighColor"] = Color.Parse ("#FFD6D6D6");
		scale.Resources["SystemControlBackgroundChromeMediumLowBrush"] = new SolidColorBrush (Color.Parse ("#FFF2F2F2"));

		var ink = ThemeInk.Foreground (scale);
		Assert.IsLessThan (0.35, Luminance (ink), ink.ToString ());
	}

	[UiTestMethod]
	public void DarkTheme_YearInk_StaysLightWhenBaseHighIsDim ()
	{
		AvaloniaApp.Ensure ();
		var scale = ShownScale (ThemeVariant.Dark);
		scale.Resources["SystemBaseHighColor"] = Color.Parse ("#FF3A3A3A");

		var ink = ThemeInk.Foreground (scale);
		Assert.IsGreaterThan (0.62, Luminance (ink), ink.ToString ());
		Assert.AreEqual (255, ink.A);
	}

	TimelineScale ShownScale (ThemeVariant theme)
	{
		var scale = new TimelineScale { Items = Months (3), Width = 300, Height = 48 };
		var window = new Window {
			Width = 340,
			Height = 90,
			Content = scale,
			RequestedThemeVariant = theme
		};
		windows.Add (window);
		window.Show ();
		window.UpdateLayout ();
		return scale;
	}

	// Alpha is painted on the theme paper. A translucent black caption is gray on a light track.
	static double Luminance (Color color)
	{
		double alpha = color.A / 255.0;
		double paper = 255;
		double r = color.R * alpha + paper * (1 - alpha);
		double g = color.G * alpha + paper * (1 - alpha);
		double b = color.B * alpha + paper * (1 - alpha);
		return (r + g + b) / (3.0 * 255);
	}

	[UiTestMethod]
	public void ShiftDrag_FromFirstMonthToThird_ExecutesInclusiveRange ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (4);
		(int Min, int Max)? range = null;
		var (strip, window) = Shown (buckets, 200, range: value => range = value);

		Assert.IsGreaterThan (0, strip.ColumnWidth);
		Point start = At (strip, window, 0);
		Point end = At (strip, window, 2);

		window.MouseDown (start, MouseButton.Left, RawInputModifiers.Shift);
		window.MouseMove (end, RawInputModifiers.Shift);
		window.MouseUp (end, MouseButton.Left, RawInputModifiers.Shift);

		Assert.AreEqual ((0, 2), range);
	}

	[UiTestMethod]
	public void Scale_RebuildsBucketsAfterItLeavesAndReturns ()
	{
		AvaloniaApp.Ensure ();
		var scale = new TimelineScale { Items = Months (3), Width = 300, Height = 48 };
		var window = new Window { Width = 320, Height = 80, Content = scale };
		windows.Add (window);
		window.Show ();
		window.UpdateLayout ();
		Assert.IsTrue (scale.Step (1));

		window.Content = new Border ();
		window.UpdateLayout ();
		window.Content = scale;
		window.UpdateLayout ();

		Assert.IsTrue (scale.Step (1));
	}

	[UiTestMethod]
	public void Scale_KeepsANarrowedRangeWhenItLeavesAndReturns ()
	{
		AvaloniaApp.Ensure ();
		var scale = new TimelineScale { Items = Months (4), Width = 400, Height = 48 };
		var window = new Window { Width = 420, Height = 80, Content = scale };
		windows.Add (window);
		window.Show ();
		window.UpdateLayout ();
		scale.ApplyRange (1, 2);
		Assert.AreEqual ((1, 2), scale.Limits);

		window.Content = new Border ();
		window.UpdateLayout ();
		window.Content = scale;
		window.UpdateLayout ();

		Assert.AreEqual ((1, 2), scale.Limits);
	}

	[UiTestMethod]
	public void Scale_KeepsLimitsWhenTheSameMonthsArriveTogether ()
	{
		AvaloniaApp.Ensure ();
		var items = new TimelineBucketCollection ();
		foreach (var month in Months (4))
			items.Add (month);
		var scale = new TimelineScale { Items = items, Width = 400, Height = 48 };
		var window = new Window { Width = 420, Height = 80, Content = scale };
		windows.Add (window);
		window.Show ();
		window.UpdateLayout ();
		scale.ApplyRange (1, 2);

		items.ReplaceAll (Months (4));

		Assert.AreEqual ((1, 2), scale.Limits);
	}

	[UiTestMethod]
	public void Months_StretchAndStayAddressable ()
	{
		AvaloniaApp.Ensure ();
		var (strip, _) = Shown (Months (3), 240);

		Assert.IsGreaterThan (TimelineChrome.MinHitPx, strip.ColumnWidth);
		Assert.AreEqual (0, strip.IndexAt (OnStrip (strip, 4)));
		Assert.AreEqual (1, strip.IndexAt (OnStrip (strip, strip.ColumnWidth + 4)));
		Assert.IsFalse (Arrow (strip, "◀").IsEnabled);
		Assert.IsFalse (Arrow (strip, "▶").IsEnabled);
	}

	[UiTestMethod]
	public void CrowdedMonths_ScrollByAStepAndKeepTheMinimumColumn ()
	{
		AvaloniaApp.Ensure ();
		var (strip, window) = Shown (Months (20), 240);
		var scroller = strip.GetVisualDescendants ().OfType<ScrollViewer> ().Single ();

		Assert.AreEqual (TimelineChrome.MinHitPx, strip.ColumnWidth);
		Assert.IsFalse (Arrow (strip, "◀").IsEnabled);
		Assert.IsTrue (Arrow (strip, "▶").IsEnabled);

		Click (window, Arrow (strip, "▶"));

		Assert.IsGreaterThan (0, scroller.Offset.X);
		Assert.IsLessThan (40, scroller.Offset.X);
		Assert.IsTrue (Arrow (strip, "◀").IsEnabled);
	}

	[UiTestMethod]
	public void EmptyMonth_KeepsAColumnAndSnapsToTheNearestPhotos ()
	{
		AvaloniaApp.Ensure ();
		var filled = Month (1, 2);
		var empty = Month (2, 0);
		var later = Month (4, 3);
		var buckets = new List<TimelineBucket> { filled, empty, Month (3, 0), later };
		var selected = new RecordingSelect (buckets);
		var (strip, window) = Shown (buckets, 240, select: selected);

		Assert.AreEqual (0, strip.IndexAt (OnStrip (strip, 4)));

		Click (window, At (strip, window, 1));

		Assert.AreEqual (1, selected.Calls);
		Assert.AreSame (filled, selected.Last);
	}

	[UiTestMethod]
	public void Click_SelectsTheMonthOnce ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (4);
		var selected = new RecordingSelect (buckets);
		var (strip, window) = Shown (buckets, 240, select: selected);

		Click (window, At (strip, window, 1));

		Assert.AreEqual (1, selected.Calls);
		Assert.AreSame (buckets[1], selected.Last);
	}

	[UiTestMethod]
	public void DraggingTheGlass_MovesOnceToTheMonthUnderThePointer ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (4);
		buckets[0].Selected = true;
		var selected = new RecordingSelect (buckets);
		var (strip, window) = Shown (buckets, 240, select: selected);

		window.MouseDown (At (strip, window, 0), MouseButton.Left);
		window.MouseMove (At (strip, window, 2));
		window.MouseMove (At (strip, window, 2, 4));
		window.MouseUp (At (strip, window, 2, 4), MouseButton.Left);

		Assert.AreEqual (1, selected.Calls);
		Assert.AreSame (buckets[2], selected.Last);
	}

	[UiTestMethod]
	public void DraggingInsideTheSelectedMonth_DoesNotClearIt ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (3);
		buckets[0].Selected = true;
		var selected = new RecordingSelect (buckets);
		var (strip, window) = Shown (buckets, 240, select: selected);

		Point start = At (strip, window, 0);
		window.MouseDown (start, MouseButton.Left);
		window.MouseMove (At (strip, window, 0, 8));
		window.MouseUp (At (strip, window, 0, 8), MouseButton.Left);

		Assert.AreEqual (0, selected.Calls);
	}

	[UiTestMethod]
	public void RightArrowKey_MovesToTheNextMonthAndScrollsItIntoView ()
	{
		AvaloniaApp.Ensure ();
		var buckets = Months (20);
		buckets[0].Selected = true;
		var selected = new RecordingSelect (buckets);
		var (strip, window) = Shown (buckets, 240, select: selected);
		var scroller = strip.GetVisualDescendants ().OfType<ScrollViewer> ().Single ();
		int steps = (int)Math.Floor (scroller.Viewport.Width / strip.ColumnWidth) + 1;
		strip.Focus ();

		for (int i = 0; i < steps; i++)
			window.KeyPress (Key.Right, RawInputModifiers.None, default, null);

		Assert.AreSame (buckets[steps], selected.Last);
		Assert.IsGreaterThan (0, scroller.Offset.X);
	}

	(TimelineStrip Strip, Window Window) Shown (
		IReadOnlyList<TimelineBucket> buckets,
		double width,
		Action<(int Min, int Max)>? range = null,
		RecordingSelect? select = null)
	{
		var strip = new TimelineStrip {
			Width = width,
			Height = 90,
			Items = buckets,
			RangeCommand = range is null ? null : new RecordingRangeCommand (range),
			SelectCommand = select
		};
		var window = new Window {
			Width = width + 40,
			Height = 120,
			Content = strip
		};
		windows.Add (window);
		window.Show ();
		strip.UpdateLayout ();
		return (strip, window);
	}

	static List<TimelineBucket> Months (int count)
	{
		var months = new List<TimelineBucket> ();
		var start = new DateTime (2024, 1, 1);
		for (int i = 0; i < count; i++) {
			var month = start.AddMonths (i);
			months.Add (new TimelineBucket {
				MonthLabel = month.ToString ("MMM"),
				YearLabel = month.Month == 1 ? month.Year.ToString () : null,
				Start = month,
				End = month.AddMonths (1),
				Count = 1,
				RelativeHeight = 1
			});
		}
		return months;
	}

	static TimelineBucket Month (int month, int count)
	{
		var start = new DateTime (2026, month, 1);
		return new TimelineBucket {
			MonthLabel = start.ToString ("MMM"),
			YearLabel = month == 1 ? "2026" : null,
			Start = start,
			End = start.AddMonths (1),
			Count = count,
			RelativeHeight = count == 0 ? 0 : 1
		};
	}

	static Point OnStrip (TimelineStrip strip, double scaleX)
	{
		var scale = strip.GetVisualDescendants ().OfType<TimelineScale> ().Single ();
		return scale.TranslatePoint (new Point (scaleX, 8), strip)
			?? throw new InvalidOperationException ("timeline has no strip point");
	}

	static Point At (TimelineStrip strip, Window window, int index, double dx = 0)
	{
		var scale = strip.GetVisualDescendants ().OfType<TimelineScale> ().Single ();
		double x = strip.ColumnWidth * index + strip.ColumnWidth / 2 + dx;
		return scale.TranslatePoint (new Point (x, 20), window)
			?? throw new InvalidOperationException ("timeline has no window point");
	}

	static Button Arrow (TimelineStrip strip, string content)
		=> strip.GetVisualDescendants ().OfType<Button> ().Single (button => Equals (button.Content, content));

	static void Click (Window window, Button button)
	{
		Point point = button.TranslatePoint (new Point (button.Bounds.Width / 2, button.Bounds.Height / 2), window)
			?? throw new InvalidOperationException ("button has no window point");
		Click (window, point);
	}

	static void Click (Window window, Point point)
	{
		window.MouseDown (point, MouseButton.Left);
		window.MouseUp (point, MouseButton.Left);
	}

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

	sealed class RecordingSelect : ICommand
	{
		readonly IReadOnlyList<TimelineBucket> buckets;

		public RecordingSelect (IReadOnlyList<TimelineBucket> buckets)
			=> this.buckets = buckets;

		public int Calls { get; private set; }
		public TimelineBucket? Last { get; private set; }

		public bool CanExecute (object? parameter) => true;

		public void Execute (object? parameter)
		{
			Calls++;
			Last = (TimelineBucket)parameter!;
			foreach (var bucket in buckets)
				bucket.Selected = ReferenceEquals (bucket, Last);
		}

		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
