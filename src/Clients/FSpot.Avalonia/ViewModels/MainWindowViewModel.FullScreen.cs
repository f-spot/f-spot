// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.ViewModels;

public partial class MainWindowViewModel : IFullScreenHost
{
	/// <summary>
	/// GTK HandleViewFullscreen / HandleViewSlideShow: the whole visible collection, starting at the focused photo.
	/// </summary>
	void ShowFullScreen (bool playSlideshow)
	{
		var paths = new List<string> ();
		int start = 0;
		foreach (var item in Photos) {
			string? path = PathOf (item);
			if (path is null)
				continue;
			if (ReferenceEquals (item, SelectedPhoto))
				start = paths.Count;
			paths.Add (path);
		}

		if (paths.Count == 0) {
			_ = shell.ShowWarningAsync (playSlideshow ? Strings.Slideshow : Strings.Fullscreen, Strings.NoSelectionAvailable);
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
		=> Photos.FirstOrDefault (item => Matches (item, path));

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

	public async Task<bool> RemovePhotoAsync (string path, bool deleteFromDrive)
	{
		if (ItemAt (path) is not { } item)
			return false;
		uint? id = item.Photo is Photo photo ? photo.Id : null;
		SelectPhoto (item);
		if (deleteFromDrive)
			await DeleteFromDriveAsync ();
		else
			await RemoveFromCatalogAsync ();
		// The edited file can disappear while the original stays. The slideshow
		// path is then no longer the default version, and the photo is still here.
		if (id is uint photoId)
			return !Photos.Any (candidate => candidate.Photo is Photo kept && kept.Id == photoId);
		return ItemAt (path) is null;
	}
}
