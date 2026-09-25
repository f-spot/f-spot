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
	Task<bool> RemovePhotoAsync (string path, bool deleteFromDrive);
}
