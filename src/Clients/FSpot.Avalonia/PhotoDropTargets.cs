// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

public static class PhotoDropTargets
{
	public static IReadOnlyList<PhotoItemViewModel> ForTagDrop (
		PhotoItemViewModel? dropTarget,
		IReadOnlyList<PhotoItemViewModel> photos)
	{
		if (photos is null || photos.Count == 0)
			return Array.Empty<PhotoItemViewModel> ();

		if (dropTarget is null || dropTarget.Selected)
			return photos.Where (photo => photo.Selected).ToList ();

		return new[] { dropTarget };
	}
}
