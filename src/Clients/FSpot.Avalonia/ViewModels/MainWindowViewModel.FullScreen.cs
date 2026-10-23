// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Resources.Lang;

using Hyena;

namespace FSpot.Avalonia.ViewModels;

public partial class MainWindowViewModel : IFullScreenHost
{
	/// <summary>
	/// GTK HandleViewFullscreen / HandleViewSlideShow: the whole visible collection, starting at the focused photo.
	/// </summary>
	void ShowFullScreen (bool playSlideshow)
	{
		List<string> paths;
		int start;
		if (Photos.IsPaged && libraryQuery is not null) {
			var aligned = libraryQuery.DefaultVersionPaths ();
			int selectedIndex = -1;
			if (SelectedPhoto?.Photo is Photo photo)
				selectedIndex = libraryQuery.IndexOf (photo);
			paths = new List<string> ();
			start = 0;
			for (int i = 0; i < aligned.Count; i++) {
				if (string.IsNullOrEmpty (aligned[i]))
					continue;
				if (i == selectedIndex)
					start = paths.Count;
				paths.Add (aligned[i]);
			}
		} else {
			paths = new List<string> ();
			start = 0;
			foreach (var item in Photos) {
				string? path = PathOf (item);
				if (path is null)
					continue;
				if (ReferenceEquals (item, SelectedPhoto))
					start = paths.Count;
				paths.Add (path);
			}
		}

		if (paths.Count == 0) {
			UiTasks.Forget (shell.ShowWarningAsync (playSlideshow ? Strings.Slideshow : Strings.Fullscreen, Strings.NoSelectionAvailable));
			return;
		}

		if (playSlideshow)
			shell.ShowSlideshow (paths, start);
		else
			shell.ShowFullScreen (paths, start);
	}

	static string? PathOf (PhotoItemViewModel item)
	{
		var paths = PhotoFileActions.SelectedPaths (new[] { item.Photo });
		return paths.Count == 0 ? null : paths[0];
	}

	PhotoItemViewModel? ItemAt (string path)
	{
		if (!Photos.IsPaged || libraryQuery is null || database is null)
			return Photos.FirstOrDefault (item => Matches (item, path));

		foreach (PhotoItemViewModel item in Photos.Realized)
			if (Matches (item, path))
				return item;

		Photo found = database.Photos.GetByUri (SafeUri.FromPath (path));
		if (found is null)
			return null;
		int index = libraryQuery.IndexOf (found);
		return index >= 0 ? Photos[index] : null;
	}

	static bool Matches (PhotoItemViewModel item, string path)
	{
		if (item.Photo is Photo photo) {
			foreach (uint versionId in photo.VersionIds) {
				string versionPath = PhotoDriveDelete.PathFor (photo, versionId);
				if (FileIdentity.Same (versionPath, path))
					return true;
			}

			return false;
		}

		string? current = PathOf (item);
		return current is not null && FileIdentity.Same (current, path);
	}

	public IPhoto? PhotoAt (string path)
		=> ItemAt (path)?.Photo;

	public void FocusPhoto (string path)
	{
		if (ItemAt (path) is { } item)
			SelectPhoto (item);
	}

	public void RatePhoto (string path, uint rating)
	{
		if (ItemAt (path) is not { } item)
			return;
		SelectPhoto (item);
		ApplyRating (rating);
		Info.Update (item.Photo);
	}

	public void RotatePhoto (string path, bool clockwise)
	{
		if (ItemAt (path) is not { } item)
			return;
		SelectPhoto (item);
		RotateSelection (clockwise ? RotateDirection.Clockwise : RotateDirection.Counterclockwise);
	}

	public async Task<CatalogRemoval> RemovePhotoAsync (string path, bool deleteFromDrive)
	{
		if (ItemAt (path) is not { } item)
			return CatalogRemoval.Unchanged;
		uint? id = item.Photo is Photo photo ? photo.Id : null;
		SelectPhoto (item);
		if (deleteFromDrive)
			await DeleteFromDriveAsync ();
		else
			await RemoveFromCatalogAsync ();
		// The edited file can disappear while the original stays. The photo is still
		// in the grid, and the show has to follow the version that survived.
		if (id is uint photoId) {
			PhotoItemViewModel? kept = null;
			if (Photos.IsPaged && libraryQuery is not null && database is not null) {
				int index = libraryQuery.IndexOf (database.Photos.Get (photoId));
				if (index >= 0)
					kept = Photos[index];
			} else {
				kept = Photos.FirstOrDefault (candidate => candidate.Photo is Photo photo && photo.Id == photoId);
			}
			if (kept is null)
				return CatalogRemoval.Gone;
			string? surviving = PathOf (kept);
			if (surviving is not null && !FileIdentity.Same (surviving, path))
				return CatalogRemoval.StayedAt (surviving);
			return CatalogRemoval.Unchanged;
		}
		return ItemAt (path) is null ? CatalogRemoval.Gone : CatalogRemoval.Unchanged;
	}
}
