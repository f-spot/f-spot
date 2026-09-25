// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Thumbnail;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FilterAndTagDialogViewModelTests
{
	[UiTestMethod]
	public void LastImportRoll_FiltersToTheNewestRollAndClearRollFilterUndoesIt ()
	{
		using var catalog = Catalog.Open ();
		var viewModel = catalog.ViewModel;
		Assert.IsFalse (viewModel.ClearRollFilterCommand.CanExecute (null));

		viewModel.FindLastImportRollCommand.Execute (null);

		Assert.IsTrue (viewModel.HasFindRoll);
		Assert.HasCount (1, viewModel.Photos);
		Assert.AreEqual ("b.jpg", viewModel.Photos[0].Caption);
		Assert.IsTrue (viewModel.ClearRollFilterCommand.CanExecute (null));

		viewModel.ClearRollFilterCommand.Execute (null);

		Assert.IsFalse (viewModel.HasFindRoll);
		Assert.HasCount (2, viewModel.Photos);
	}

	[UiTestMethod]
	public async Task SelectImportRolls_OffersEveryRollWithItsPhotoCount ()
	{
		using var catalog = Catalog.Open ();
		catalog.Shell.RollIds = [catalog.FirstRoll];

		await catalog.ViewModel.FindByImportRollCommand.ExecuteAsync (null);

		Assert.IsNotNull (catalog.Shell.OfferedRolls);
		Assert.HasCount (2, catalog.Shell.OfferedRolls);
		Assert.IsTrue (catalog.Shell.OfferedRolls.All (roll => roll.PhotoCount == 1));
		Assert.HasCount (1, catalog.ViewModel.Photos);
		Assert.AreEqual ("a.jpg", catalog.ViewModel.Photos[0].Caption);
	}

	[UiTestMethod]
	public async Task RatingAndDateDialogs_SetAndClearTheirFilters ()
	{
		using var catalog = Catalog.Open ();
		var viewModel = catalog.ViewModel;

		catalog.Shell.Rating = (4, 5);
		await viewModel.FindByRatingCommand.ExecuteAsync (null);
		Assert.AreEqual ((4u, 5u), catalog.Shell.RatingOffered);
		Assert.IsTrue (viewModel.ClearRatingFilterCommand.CanExecute (null));
		viewModel.ClearRatingFilterCommand.Execute (null);
		Assert.IsFalse (viewModel.HasFindRating);

		catalog.Shell.Dates = new DateRangeChoice ((new DateTime (2000, 1, 1), new DateTime (2000, 1, 2)));
		await viewModel.FindByDateCommand.ExecuteAsync (null);
		Assert.IsTrue (viewModel.HasFindDate);
		Assert.IsTrue (viewModel.ClearDateRangeCommand.CanExecute (null));

		catalog.Shell.Dates = new DateRangeChoice (null);
		await viewModel.FindByDateCommand.ExecuteAsync (null);
		Assert.IsFalse (viewModel.HasFindDate);
		Assert.IsFalse (viewModel.ClearDateRangeCommand.CanExecute (null));
	}

	[UiTestMethod]
	public async Task CreateTag_UsesTheDialogsParentAndRemembersAutoIcon ()
	{
		using var catalog = Catalog.Open ();
		bool autoIcon = Preferences.Get<bool> (Preferences.TagIconAutomatic);
		try {
			catalog.Shell.Create = root => new CreateTagChoice ("Kids", catalog.Tag, false);

			await catalog.ViewModel.CreateTagCommand.ExecuteAsync (null);

			var kids = catalog.Database.Tags.GetTagByName ("Kids");
			Assert.IsNotNull (kids);
			Assert.AreSame (catalog.Tag, kids.Category);
			Assert.IsFalse (Preferences.Get<bool> (Preferences.TagIconAutomatic));
		} finally {
			Preferences.Set (Preferences.TagIconAutomatic, autoIcon);
		}
	}

	[UiTestMethod]
	public async Task EditTag_RenamesReparentsAndSetsTheIcon ()
	{
		using var catalog = Catalog.Open ();
		var other = catalog.Database.Tags.CreateCategory (catalog.Database.Tags.RootCategory, "Other", false);
		catalog.ViewModel.LoadCatalog ();
		catalog.Shell.Edit = source => new EditTagChoice ("Renamed", other, new TagIconChoice ("emblem-people", null));

		await catalog.ViewModel.EditTagCommand.ExecuteAsync (new TagNode (catalog.Tag));

		Assert.AreEqual ("Renamed", catalog.Tag.Name);
		Assert.AreSame (other, catalog.Tag.Category);
		Assert.AreEqual ("emblem-people", catalog.Tag.ThemeIconName);
		Assert.IsNotNull (catalog.Shell.EditedSource);
		Assert.HasCount (1, catalog.Shell.EditedSource.PhotoPaths);
	}

	[UiTestMethod]
	public async Task MergeTags_FoldsTheHighlightedTagsIntoOne ()
	{
		using var catalog = Catalog.Open ();
		var second = catalog.Database.Tags.CreateCategory (catalog.Database.Tags.RootCategory, "Second", false);
		var b = catalog.Database.Photos.Query (new FSpot.Query.RollSet (catalog.Database.Rolls.Get (catalog.SecondRoll))).Single ();
		b.AddTag (second);
		catalog.Database.Photos.Commit (b);
		catalog.ViewModel.LoadCatalog ();
		catalog.ViewModel.SetHighlightedTags ([new TagNode (catalog.Tag), new TagNode (second)]);
		catalog.Shell.Confirm = true;

		Assert.IsTrue (catalog.ViewModel.MergeTagsCommand.CanExecute (null));
		await catalog.ViewModel.MergeTagsCommand.ExecuteAsync (null);

		Assert.IsNull (catalog.Database.Tags.GetTagByName ("Tagged"));
		var survivor = catalog.Database.Tags.GetTagByName ("Second");
		Assert.IsNotNull (survivor);
		var tagged = catalog.Database.Photos.Query (new FSpot.Query.TagTerm (survivor));
		Assert.HasCount (2, tagged);
		Assert.AreSame (survivor, catalog.Shell.EditedSource?.Tag);
	}

	[UiTestMethod]
	public async Task MergeTags_WithOneHighlightedTagAsksTheTagSelectionDialog ()
	{
		using var catalog = Catalog.Open ();
		catalog.ViewModel.SetHighlightedTags ([new TagNode (catalog.Tag)]);

		await catalog.ViewModel.MergeTagsCommand.ExecuteAsync (null);

		Assert.IsTrue (catalog.Shell.PickedTags);
		Assert.IsNotNull (catalog.Database.Tags.GetTagByName ("Tagged"));
	}

	[UiTestMethod]
	public async Task RenameVersion_OffersTheCurrentAndExistingNames ()
	{
		using var catalog = Catalog.Open ();
		catalog.ViewModel.Photos[0].Selected = true;

		await catalog.ViewModel.RenameVersionCommand.ExecuteAsync (null);

		Assert.AreEqual (VersionNameRequest.Rename, catalog.Shell.VersionRequest);
		Assert.IsNotNull (catalog.Shell.VersionNames);
		Assert.Contains (catalog.Shell.VersionCurrent!, catalog.Shell.VersionNames);
	}

	sealed class Catalog : IDisposable
	{
		readonly string root;

		Catalog (string root, Db database, DialogShell shell, MainWindowViewModel viewModel, Category tag, uint firstRoll, uint secondRoll)
		{
			this.root = root;
			Database = database;
			Shell = shell;
			ViewModel = viewModel;
			Tag = tag;
			FirstRoll = firstRoll;
			SecondRoll = secondRoll;
		}

		public Db Database { get; }
		public DialogShell Shell { get; }
		public MainWindowViewModel ViewModel { get; }
		public Category Tag { get; }
		public uint FirstRoll { get; }
		public uint SecondRoll { get; }

		public static Catalog Open ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-dialogs-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			File.Copy (sample, Path.Combine (root, "a.jpg"));
			File.Copy (sample, Path.Combine (root, "b.jpg"));

			var database = new Db (new UnusedImages (), new UnusedThumbs (), new UnusedUpdater ());
			database.Init (Path.Combine (root, "photos.db"), true);
			var tag = database.Tags.CreateCategory (database.Tags.RootCategory, "Tagged", false);
			uint first = database.Rolls.Create (DateTime.UtcNow.AddDays (-1)).Id;
			uint second = database.Rolls.Create (DateTime.UtcNow).Id;
			var a = database.Photos.CreateFrom (new FilePhoto (new SafeUri (Path.Combine (root, "a.jpg"))), true, first);
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (Path.Combine (root, "b.jpg"))), true, second);
			a.AddTag (tag);
			database.Photos.Commit (a);

			var shell = new DialogShell ();
			var viewModel = new MainWindowViewModel (database, shell, new NullThumbs ());
			viewModel.LoadCatalog ();
			return new Catalog (root, database, shell, viewModel, tag, first, second);
		}

		public void Dispose ()
		{
			Database.Dispose ();
			Directory.Delete (root, true);
		}
	}

	sealed class DialogShell : IAvaloniaShell
	{
		public (uint Min, uint Max)? Rating { get; set; }
		public (uint Min, uint Max)? RatingOffered { get; private set; }
		public DateRangeChoice? Dates { get; set; }
		public IReadOnlyList<uint>? RollIds { get; set; }
		public IReadOnlyList<ImportRollOption>? OfferedRolls { get; private set; }
		public Func<Category, CreateTagChoice?>? Create { get; set; }
		public Func<TagIconSource, EditTagChoice?>? Edit { get; set; }
		public TagIconSource? EditedSource { get; private set; }
		public bool Confirm { get; set; }
		public bool PickedTags { get; private set; }
		public VersionNameRequest? VersionRequest { get; private set; }
		public string? VersionCurrent { get; private set; }
		public IReadOnlyCollection<string>? VersionNames { get; private set; }

		public void Close () { }
		public void ToggleFullscreen () { }
		public void FocusFind () { }
		public Task ShowAboutAsync () => Task.CompletedTask;
		public Task ShowWarningAsync (string title, string message) => Task.CompletedTask;
		public Task<bool> ShowImportAsync () => Task.FromResult (false);
		public Task<bool> ShowImportAsync (IReadOnlyList<string> paths) => Task.FromResult (false);
		public Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (Confirm);
		public Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null) => Task.CompletedTask;
		public Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) { }

		public Task<(uint Min, uint Max)?> PromptRatingFilterAsync (uint min, uint max)
		{
			RatingOffered = (min, max);
			return Task.FromResult (Rating);
		}

		public Task<DateRangeChoice?> PromptDateRangeFilterAsync (DateTime? start, DateTime? end)
			=> Task.FromResult (Dates);

		public Task<IReadOnlyList<uint>?> PickImportRollsAsync (IReadOnlyList<ImportRollOption> rolls)
		{
			OfferedRolls = rolls;
			return Task.FromResult (RollIds);
		}

		public Task<CreateTagChoice?> PromptCreateTagAsync (Category root, Category parent, bool autoIcon)
			=> Task.FromResult (Create?.Invoke (root));

		public Task<EditTagChoice?> PromptEditTagAsync (Category root, TagIconSource tag)
		{
			EditedSource = tag;
			return Task.FromResult (Edit?.Invoke (tag));
		}

		public Task<string?> PromptVersionNameAsync (VersionNameRequest request, string currentName, IReadOnlyCollection<string> existingNames)
		{
			VersionRequest = request;
			VersionCurrent = currentName;
			VersionNames = existingNames;
			return Task.FromResult<string?> (null);
		}

		public Task<IReadOnlyList<Tag>?> PickTagsAsync (Category root)
		{
			PickedTags = true;
			return Task.FromResult<IReadOnlyList<Tag>?> (null);
		}
	}

	sealed class NullThumbs : IPhotoThumbnailLoader
	{
		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
			=> Task.FromResult<Bitmap?> (null);
	}

	sealed class UnusedUpdater : IUpdaterUI
	{
		public void Show () { }
		public void Pulse () { }
		public void Destroy () { }
	}

	sealed class UnusedImages : IImageFileFactory
	{
		public IImageFile Create (SafeUri uri) => throw new NotSupportedException ();
		public bool HasLoader (SafeUri uri) => false;
		public bool IsJpeg (SafeUri uri) => false;
		public bool IsRaw (SafeUri uri) => false;
		public bool IsJpegRawPair (SafeUri file1, SafeUri file2) => false;
		public void RegisterGenericLoader (string extension) { }
	}

	sealed class UnusedThumbs : IThumbnailService
	{
		public ImageBuffer GetThumbnail (SafeUri fileUri, ThumbnailSize size) => throw new NotSupportedException ();
		public ImageBuffer TryLoadThumbnail (SafeUri fileUri, ThumbnailSize size) => throw new NotSupportedException ();
		public void DeleteThumbnails (SafeUri fileUri) { }
	}
}
