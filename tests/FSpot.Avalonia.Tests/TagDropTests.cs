// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TagDropTests
{
	[TestMethod]
	public void AttachDroppedTag_OnAnUnselectedPhoto_TagsOnlyThatPhoto ()
	{
		using var catalog = Catalog.Open ();
		var dropped = catalog.ViewModel.Photos.Single (photo => photo.Caption == "b.jpg");
		catalog.ViewModel.Photos.Single (photo => photo.Caption == "a.jpg").Selected = true;

		catalog.ViewModel.AttachDroppedTag (catalog.Tag.Id.ToString (), dropped);

		Assert.Contains ("Dropped", dropped.TagsCaption);
		Assert.DoesNotContain ("Dropped", catalog.ViewModel.Photos.Single (photo => photo.Caption == "a.jpg").TagsCaption);
		Assert.IsTrue (catalog.HasStoredTag (dropped));
		Assert.IsFalse (catalog.HasStoredTag (catalog.ViewModel.Photos.Single (photo => photo.Caption == "a.jpg")));
	}

	[TestMethod]
	public void AttachDroppedTag_OnASelectedPhoto_TagsTheWholeSelection ()
	{
		using var catalog = Catalog.Open ();
		foreach (var photo in catalog.ViewModel.Photos)
			photo.Selected = true;

		catalog.ViewModel.AttachDroppedTag (catalog.Tag.Id.ToString (), catalog.ViewModel.Photos[0]);

		Assert.IsTrue (catalog.ViewModel.Photos.All (photo => photo.TagsCaption.Contains ("Dropped", StringComparison.Ordinal)));
		Assert.IsTrue (catalog.ViewModel.Photos.All (catalog.HasStoredTag));
	}

	[TestMethod]
	public void AttachDroppedTag_RejectsAMissingOrUnknownTag ()
	{
		using var catalog = Catalog.Open ();
		var photo = catalog.ViewModel.Photos[0];

		catalog.ViewModel.AttachDroppedTag (null, photo);
		catalog.ViewModel.AttachDroppedTag ("nope", photo);
		catalog.ViewModel.AttachDroppedTag ("999999", photo);

		Assert.DoesNotContain ("Dropped", photo.TagsCaption);
		Assert.IsFalse (catalog.HasStoredTag (photo));
	}

	[TestMethod]
	public void AttachDroppedTag_WithNoTargetAndNoSelection_DoesNothing ()
	{
		using var catalog = Catalog.Open ();

		catalog.ViewModel.AttachDroppedTag (catalog.Tag.Id.ToString (), null);

		Assert.IsTrue (catalog.ViewModel.Photos.All (photo => !catalog.HasStoredTag (photo)));
	}

	sealed class Catalog : IDisposable
	{
		public Db Database { get; }
		public MainWindowViewModel ViewModel { get; }
		public Tag Tag { get; }
		readonly string root;

		Catalog (string root, Db database, MainWindowViewModel viewModel, Tag tag)
		{
			this.root = root;
			Database = database;
			ViewModel = viewModel;
			Tag = tag;
		}

		public static Catalog Open ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-tagdrop-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
			File.Copy (sample, Path.Combine (root, "a.jpg"));
			File.Copy (sample, Path.Combine (root, "b.jpg"));

			var database = new Db (new UnusedImages (), new UnusedThumbs (), new UnusedUpdater ());
			database.Init (Path.Combine (root, "photos.db"), true);
			var tag = database.Tags.CreateCategory (database.Tags.RootCategory, "Dropped", false);
			uint roll = database.Rolls.Create ().Id;
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (Path.Combine (root, "a.jpg"))), true, roll);
			database.Photos.CreateFrom (new FilePhoto (new SafeUri (Path.Combine (root, "b.jpg"))), true, roll);

			var viewModel = new MainWindowViewModel (database, new Shell (), new NullThumbs ());
			viewModel.LoadCatalog ();
			return new Catalog (root, database, viewModel, tag);
		}

		public bool HasStoredTag (PhotoItemViewModel photo)
		{
			uint photoId = ((Photo)photo.Photo).Id;
			using var reader = Database.Database.Query (new DatabaseCommand (
				"SELECT 1 FROM photo_tags WHERE photo_id = ? AND tag_id = ?", photoId, Tag.Id));
			return reader.Read ();
		}

		public void Dispose ()
		{
			Database.Dispose ();
			Directory.Delete (root, true);
		}
	}

	sealed class Shell : IAvaloniaShell
	{
		public void Close () { }
		public void ToggleFullscreen () { }
		public void FocusFind () { }
		public Task ShowAboutAsync () => Task.CompletedTask;
		public Task ShowWarningAsync (string title, string message) => Task.CompletedTask;
		public Task<bool> ShowImportAsync () => Task.FromResult (false);
		public Task<bool> ShowImportAsync (IReadOnlyList<string> paths) => Task.FromResult (false);
		public Task<string?> PickFolderAsync (string title) => Task.FromResult<string?> (null);
		public Task<string?> PromptAsync (string title, string prompt, string defaultValue) => Task.FromResult<string?> (null);
		public Task<bool> ConfirmAsync (string title, string message, string okCaption) => Task.FromResult (false);
		public Task<(uint Min, uint Max)?> PromptRatingRangeAsync () => Task.FromResult<(uint, uint)?> (null);
		public Task<(DateTime Start, DateTime End)?> PromptDateRangeAsync () => Task.FromResult<(DateTime, DateTime)?> (null);
		public Task<uint?> PickImportRollAsync (IReadOnlyList<(uint Id, string Label)> rolls) => Task.FromResult<uint?> (null);
		public Task PrintPhotosAsync (IReadOnlyList<string> paths, PrintPageSetup? setup = null) => Task.CompletedTask;
		public Task MailPhotosAsync (IReadOnlyList<string> paths) => Task.CompletedTask;
		public void ShowSlideshow (IReadOnlyList<string> paths, int startIndex) { }
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
