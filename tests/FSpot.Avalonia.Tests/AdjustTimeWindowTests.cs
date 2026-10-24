// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AdjustTimeWindowTests
{
	[UiTestMethod]
	public void TryParseHours_AcceptsASignedNumber ()
	{
		Assert.IsTrue (AdjustTimeWindow.TryParseHours ("1.5", out double hours));
		Assert.AreEqual (1.5, hours);
		Assert.IsTrue (AdjustTimeWindow.TryParseHours ("-3", out hours));
		Assert.AreEqual (-3, hours);
	}

	[UiTestMethod]
	public void TryParseHours_RejectsBlankAndWords ()
	{
		Assert.IsFalse (AdjustTimeWindow.TryParseHours ("  ", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseHours ("noon", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseHours (null, out _));
	}

	[UiTestMethod]
	public void TryParseOffset_AcceptsATimeSpan ()
	{
		Assert.IsTrue (AdjustTimeWindow.TryParseOffset ("01:30:00", out TimeSpan offset));
		Assert.AreEqual (TimeSpan.FromMinutes (90), offset);
		Assert.IsTrue (AdjustTimeWindow.TryParseOffset ("-00:15:00", out offset));
		Assert.AreEqual (TimeSpan.FromMinutes (-15), offset);
	}

	[UiTestMethod]
	public void TryParseOffset_RejectsBlankAndWords ()
	{
		Assert.IsFalse (AdjustTimeWindow.TryParseOffset ("  ", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseOffset ("soon", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseOffset (null, out _));
	}

	[UiTestMethod]
	public void FormatOffset_RoundTripsThroughTryParseOffset ()
	{
		var offset = new TimeSpan (1, 2, 3);
		string text = AdjustTimeWindow.FormatOffset (offset);
		Assert.IsTrue (AdjustTimeWindow.TryParseOffset (text, out TimeSpan parsed));
		Assert.AreEqual (offset, parsed);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Constructor_ShowsTheSelectedRowAsTheReferencePhoto ()
	{
		AvaloniaApp.Ensure ();
		var photos = new (string Name, DateTime Time, string Path)[] {
			("first.jpg", new DateTime (2020, 1, 1, 9, 0, 0), ""),
			("second.jpg", new DateTime (2020, 1, 2, 9, 0, 0), ""),
			("third.jpg", new DateTime (2020, 1, 3, 9, 0, 0), "")
		};
		var window = new AdjustTimeWindow (photos);
		window.Show ();
		try {
			Assert.AreEqual ("first.jpg", window.PhotoName.Text);
			Assert.AreEqual ("2020-01-01 09:00:00", window.OldTime.Text);
			Assert.AreEqual (string.Format (Strings.PhotoOutOfPhotos, 1, 3), window.CountLabel.Text);
			Assert.IsTrue (window.CounterRow.IsVisible);
			Assert.IsTrue (window.ActionSection.IsVisible);
			Assert.IsTrue (window.TraySection.IsVisible);
			Assert.AreEqual (string.Format (Strings.ShiftAllPhotosByX, AdjustTimeWindow.FormatOffset (TimeSpan.Zero)), window.ShiftAll.Content);
			Assert.AreEqual (string.Format (Strings.MinStartingAtX, "first.jpg"), window.StartingAtLabel.Text);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Constructor_HidesTheSpinnerTrayAndActionForASinglePhoto ()
	{
		AvaloniaApp.Ensure ();
		var photos = new (string Name, DateTime Time, string Path)[] { ("only.jpg", new DateTime (2020, 1, 1), "") };
		var window = new AdjustTimeWindow (photos);
		window.Show ();
		try {
			Assert.IsFalse (window.CounterRow.IsVisible);
			Assert.IsFalse (window.ActionSection.IsVisible);
			Assert.IsFalse (window.TraySection.IsVisible);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void DifferenceBox_UpdatesTheAdjustedDateAndTheShiftLabel ()
	{
		AvaloniaApp.Ensure ();
		var photos = new (string Name, DateTime Time, string Path)[] { ("a.jpg", new DateTime (2020, 6, 1, 12, 0, 0), "") };
		var window = new AdjustTimeWindow (photos);
		window.Show ();
		try {
			window.DifferenceBox.Text = "02:00:00";

			Assert.AreEqual (string.Format (Strings.ShiftAllPhotosByX, AdjustTimeWindow.FormatOffset (TimeSpan.FromHours (2))), window.ShiftAll.Content);
			Assert.AreEqual (new DateTimeOffset (new DateTime (2020, 6, 1), TimeSpan.Zero), window.AdjustedDatePart.SelectedDate);
			Assert.AreEqual (new TimeSpan (14, 0, 0), window.AdjustedTimePart.SelectedTime);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void SelectingATrayRow_MovesTheSpinnerToMatch ()
	{
		AvaloniaApp.Ensure ();
		var photos = new (string Name, DateTime Time, string Path)[] {
			("first.jpg", new DateTime (2020, 1, 1), ""),
			("second.jpg", new DateTime (2020, 1, 2), "")
		};
		var window = new AdjustTimeWindow (photos);
		window.Show ();
		try {
			window.PhotoList.SelectedIndex = 1;

			Assert.AreEqual ("second.jpg", window.PhotoName.Text);
			Assert.AreEqual (2m, window.PhotoSpin.Value);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void Ok_WhenSpacing_AnchorsOnTheSelectedRowNotRowZero ()
	{
		AvaloniaApp.Ensure ();
		var photos = new (string Name, DateTime Time, string Path)[] {
			("first.jpg", new DateTime (2020, 1, 1, 8, 0, 0), ""),
			("second.jpg", new DateTime (2020, 1, 2, 8, 0, 0), ""),
			("third.jpg", new DateTime (2020, 1, 3, 8, 0, 0), "")
		};
		var window = new AdjustTimeWindow (photos);
		window.Show ();
		try {
			window.PhotoList.SelectedIndex = 1;
			window.SpaceAll.IsChecked = true;
			window.SpacingBox.Text = "10";

			window.OkButton.RaiseEvent (new RoutedEventArgs (Button.ClickEvent));

			Assert.IsNotNull (window.Choice);
			Assert.AreEqual (1, window.Choice!.AnchorIndex);
			Assert.AreEqual (photos[1].Time, window.Choice.AnchorTime);
			Assert.AreEqual (10, window.Choice.SpacingMinutes);

			// The bug this dialog used to have: preview (and, before the fix, OK) treated row 0
			// as the anchor. Confirm the anchor the dialog reports really is the selected row's.
			Assert.AreNotEqual (photos[0].Time, window.Choice.AnchorTime);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void ReferencePhotoLabel_StripsTheGtkBoldMarkup ()
	{
		Assert.AreEqual ("Reference Photo", AdjustTimeLabels.ReferencePhoto);
		Assert.AreEqual ("Action", AdjustTimeLabels.Action);
		Assert.AreEqual ("Photos", AdjustTimeLabels.Photos);
		Assert.AreEqual ("Reference Photo", GtkMarkupText.StripBold (Strings.BReferencePhotoB));
	}
}
