// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Input;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

public sealed partial class MainWindowViewModelTests
{
	static MainWindowViewModel SeededViewModel (int count)
	{
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), new NullThumbnails ());
		var photos = new IPhoto[count];
		for (int i = 0; i < count; i++)
			photos[i] = new StubPhoto ($"/photos/{i}.jpg", new DateTime (2026, 1, 1).AddDays (-i));
		viewModel.SeedCatalog (photos);
		return viewModel;
	}

	[UiTestMethod]
	public void SetTimelineLimits_KeepsOnlyTheMonthsBetweenTheHandles ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (
			new StubPhoto ("/jan.jpg", new DateTime (2026, 1, 10)),
			new StubPhoto ("/mar.jpg", new DateTime (2026, 3, 10)),
			new StubPhoto ("/jun.jpg", new DateTime (2026, 6, 10)));
		int last = viewModel.TimelineBuckets.Count - 1;

		viewModel.SetTimelineLimitsCommand.Execute ((2, last));

		Assert.HasCount (2, viewModel.Photos);
		Assert.DoesNotContain ("jan.jpg", viewModel.Photos.Select (item => item.Caption).ToList ());
		Assert.IsTrue (viewModel.HasTimelineLimits);

		viewModel.SetTimelineLimitsCommand.Execute ((0, last));

		Assert.HasCount (3, viewModel.Photos);
		Assert.IsFalse (viewModel.HasTimelineLimits);
	}

	[UiTestMethod]
	public void SetTimelineLimits_CombinesWithAMonthSelection ()
	{
		var viewModel = NewViewModel ();
		viewModel.SeedCatalog (
			new StubPhoto ("/jan.jpg", new DateTime (2026, 1, 10)),
			new StubPhoto ("/feb.jpg", new DateTime (2026, 2, 10)),
			new StubPhoto ("/mar.jpg", new DateTime (2026, 3, 10)));
		viewModel.SetTimelineLimitsCommand.Execute ((1, 2));
		viewModel.SelectTimelineCommand.Execute (viewModel.TimelineBuckets[0]);

		Assert.IsEmpty (viewModel.Photos);
		Assert.IsTrue (viewModel.ShowNoMatches);
	}

	[UiTestMethod]
	public void ApplyGridSelection_SetsTheFlagsAndTheCurrentPhoto ()
	{
		var viewModel = SeededViewModel (5);

		viewModel.ApplyGridSelection (new HashSet<int> { 1, 3 }, 3);

		Assert.AreSequenceEqual (new[] { false, true, false, true, false }, viewModel.Photos.Select (item => item.Selected).ToArray ());
		Assert.AreSame (viewModel.Photos[3], viewModel.SelectedPhoto);
		Assert.AreSequenceEqual (new[] { 1, 3 }, viewModel.SelectedIndices ().Order ().ToArray ());
	}

	[UiTestMethod]
	public void RunViewerAction_WalksThePhotos ()
	{
		var viewModel = SeededViewModel (4);
		viewModel.SelectPhoto (viewModel.Photos[1]);

		viewModel.RunViewerAction (ViewerAction.Last, 400, 300, 200, 150);
		Assert.AreSame (viewModel.Photos[3], viewModel.SelectedPhoto);
		viewModel.RunViewerAction (ViewerAction.Previous, 400, 300, 200, 150);
		Assert.AreSame (viewModel.Photos[2], viewModel.SelectedPhoto);
		viewModel.RunViewerAction (ViewerAction.First, 400, 300, 200, 150);
		Assert.AreSame (viewModel.Photos[0], viewModel.SelectedPhoto);
		viewModel.RunViewerAction (ViewerAction.Previous, 400, 300, 200, 150);
		Assert.AreSame (viewModel.Photos[0], viewModel.SelectedPhoto);
		viewModel.RunViewerAction (ViewerAction.Random, 400, 300, 200, 150);
		Assert.IsNotNull (viewModel.SelectedPhoto);
		Assert.AreNotSame (viewModel.Photos[3], viewModel.SelectedPhoto);
	}

	[UiTestMethod]
	public void RunViewerAction_ZoomsPansAndFits ()
	{
		var viewModel = SeededViewModel (1);
		Assert.IsFalse (viewModel.IsPreviewZoomed);

		viewModel.RunViewerAction (ViewerAction.ZoomIn, 400, 300, 200, 150);
		Assert.AreEqual (1.1, viewModel.PreviewZoom, 1e-9);
		Assert.IsTrue (viewModel.IsPreviewZoomed);

		viewModel.RunViewerAction (ViewerAction.PanRight, 400, 300, 200, 150);
		Assert.AreEqual (-32, viewModel.PreviewPanX, 1e-9);
		viewModel.RunViewerAction (ViewerAction.PanUp, 400, 300, 200, 150);
		Assert.AreEqual (32, viewModel.PreviewPanY, 1e-9);

		viewModel.RunViewerAction (ViewerAction.ZoomFit, 400, 300, 200, 150);
		Assert.AreEqual (1, viewModel.PreviewZoom);
		Assert.AreEqual (0, viewModel.PreviewPanX);
		Assert.AreEqual (0, viewModel.PreviewPanY);
	}

	[UiTestMethod]
	public void StepRating_StepsWithinRange ()
	{
		var viewModel = SeededViewModel (1);
		viewModel.SelectPhoto (viewModel.Photos[0]);

		viewModel.StepRating (1);
		viewModel.StepRating (1);
		Assert.AreEqual (2u, viewModel.Info.RatingValue);
		viewModel.StepRating (-1);
		viewModel.StepRating (-1);
		viewModel.StepRating (-1);
		Assert.AreEqual (0u, viewModel.Info.RatingValue);
	}

	[UiTestMethod]
	public void CommitRenameTag_WarnsWhenTheNameBelongsToAnotherTag ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var beach = new TagNode (new Tag (null, 1, "Beach"));
		var sea = new TagNode (new Tag (null, 2, "Sea"));
		viewModel.Tags.Add (beach);
		viewModel.Tags.Add (sea);

		viewModel.BeginRenameTagCommand.Execute (sea);
		sea.Name = "beach";
		viewModel.CommitRenameTagCommand.Execute (sea);

		Assert.AreEqual ("Sea", sea.Tag.Name);
		Assert.AreEqual ("Sea", sea.Name);
		Assert.IsFalse (sea.IsRenaming);
		Assert.AreEqual (Strings.ErrorRenamingTag, shell.WarningTitle);
		Assert.AreEqual (Strings.ThisNameIsAlreadyInUse, shell.WarningMessage);
	}

	[UiTestMethod]
	public void CommitRenameTag_AllowsACaseChange ()
	{
		var shell = new RecordingShell ();
		var viewModel = new MainWindowViewModel (null, shell, new NullThumbnails ());
		var beach = new TagNode (new Tag (null, 1, "Beach"));
		viewModel.Tags.Add (beach);

		beach.Name = "beach";
		viewModel.CommitRenameTagCommand.Execute (beach);

		Assert.AreEqual ("beach", beach.Tag.Name);
		Assert.IsNull (shell.WarningTitle);
	}

	[UiTestMethod]
	public void TagNamesStartingWith_MatchesThePrefixIgnoringCase ()
	{
		var viewModel = NewViewModel ();
		viewModel.Tags.Add (new TagNode (new Tag (null, 1, "Vacation")));
		viewModel.Tags.Add (new TagNode (new Tag (null, 2, "Family")));

		Assert.AreSequenceEqual (new[] { "Vacation" }, viewModel.TagNamesStartingWith ("va")!.ToArray ());
		Assert.IsNull (viewModel.TagNamesStartingWith ("zz"));
	}

	[UiTestMethod]
	public void IncludeTagsInQuery_AddsEachTagToTheFind ()
	{
		var viewModel = NewViewModel ();
		var a = new TagNode (new Tag (null, 1, "A"));
		var b = new TagNode (new Tag (null, 2, "B"));

		viewModel.IncludeTagsInQuery ([a, b]);

		Assert.AreSequenceEqual (new[] { a, b }, viewModel.FindTags.ToArray ());
	}

	[UiTestMethod]
	public void DisplayFilenames_IsSavedToThePreferences ()
	{
		bool previous = Preferences.Get<bool> (Preferences.ViewerShawFilenames);
		try {
			var viewModel = NewViewModel ();
			viewModel.DisplayFilenames = !previous;
			Assert.AreEqual (!previous, Preferences.Get<bool> (Preferences.ViewerShawFilenames));
		} finally {
			Preferences.Set (Preferences.ViewerShawFilenames, previous);
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public async Task PreviewPopup_ShowsTheHistogramOnlyForShiftV ()
	{
		AvaloniaApp.Ensure ();
		var viewModel = new MainWindowViewModel (null, new RecordingShell (), new BitmapThumbnails ());
		viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"));

		await viewModel.ShowPreviewPopupAsync (viewModel.Photos[0], histogram: false, 1920, 1080);
		Assert.IsTrue (viewModel.ShowPreviewPopup);
		Assert.IsNotNull (viewModel.PreviewPopupImage);
		Assert.IsNull (viewModel.PreviewPopupHistogram);
		Assert.AreEqual (500, viewModel.PreviewPopupEdge);

		await viewModel.ShowPreviewPopupAsync (viewModel.Photos[1], histogram: true, 1920, 1080);
		Assert.AreSame (viewModel.Photos[1], viewModel.PreviewPopupItem);
		Assert.IsNotNull (viewModel.PreviewPopupHistogram);

		viewModel.HidePreviewPopup ();
		Assert.IsFalse (viewModel.ShowPreviewPopup);
		Assert.IsNull (viewModel.PreviewPopupImage);
		Assert.IsNull (viewModel.PreviewPopupItem);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void PhotoGrid_KeepsAMultipleSelectionFromTheKeyboard ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			var photos = new IPhoto[12];
			for (int i = 0; i < photos.Length; i++)
				photos[i] = new StubPhoto ($"/photos/{i}.jpg");
			viewModel.SeedCatalog (photos);
			Dispatcher.UIThread.RunJobs ();
			var grid = window.GetVisualDescendants ().OfType<ListBox> ()
				.Single (list => AutomationProperties.GetAutomationId (list) == AutomationIds.PhotoGrid);
			viewModel.ApplyGridSelection (new HashSet<int> { 0 }, 0);
			Dispatcher.UIThread.RunJobs ();

			grid.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.L, KeyModifiers = KeyModifiers.Shift, Source = grid });
			grid.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.L, KeyModifiers = KeyModifiers.Shift, Source = grid });
			Dispatcher.UIThread.RunJobs ();

			Assert.AreSequenceEqual (new[] { 0, 1, 2 }, viewModel.SelectedIndices ().Order ().ToArray ());
			Assert.AreSame (viewModel.Photos[2], viewModel.SelectedPhoto);

			grid.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End, Source = grid });
			Dispatcher.UIThread.RunJobs ();

			Assert.AreSequenceEqual (new[] { 11 }, viewModel.SelectedIndices ().Order ().ToArray ());
			Assert.AreSame (viewModel.Photos[11], viewModel.SelectedPhoto);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void PhotoView_KeysWalkThePhotosFromTheWindow ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"), new StubPhoto ("/c.jpg"));
			viewModel.OpenPhotoCommand.Execute (viewModel.Photos[0]);
			Dispatcher.UIThread.RunJobs ();

			window.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.N, Source = window });
			Assert.AreSame (viewModel.Photos[1], viewModel.SelectedPhoto);
			window.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End, Source = window });
			Assert.AreSame (viewModel.Photos[2], viewModel.SelectedPhoto);
			window.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Back, Source = window });
			Assert.AreSame (viewModel.Photos[1], viewModel.SelectedPhoto);
			window.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.OemPlus, Source = window });
			Assert.IsTrue (viewModel.IsPreviewZoomed);
			window.RaiseEvent (new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.D0, Source = window });
			Assert.IsFalse (viewModel.IsPreviewZoomed);
		} finally {
			window.Close ();
		}
	}
}
