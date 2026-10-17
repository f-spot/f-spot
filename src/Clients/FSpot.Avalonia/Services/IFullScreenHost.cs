// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Avalonia.Services;

/// <summary>
/// What the fullscreen view asks of the main window, which owns the catalog. Photos are identified by the
/// path the view was opened with.
/// </summary>
public interface IFullScreenHost
{
	IPhoto? PhotoAt (string path);
	void FocusPhoto (string path);
	void RatePhoto (string path, uint rating);
	void RotatePhoto (string path, bool clockwise);
	Task<CatalogRemoval> RemovePhotoAsync (string path, bool deleteFromDrive);
}

/// <summary>
/// What a fullscreen delete did. <see cref="Path"/> is the file the show should use when the photo
/// stayed in the catalog under a different version.
/// </summary>
public readonly record struct CatalogRemoval (bool LeftGrid, string? Path)
{
	public static CatalogRemoval Gone => new (true, null);

	public static CatalogRemoval Unchanged => new (false, null);

	public static CatalogRemoval StayedAt (string path) => new (false, path);
}
