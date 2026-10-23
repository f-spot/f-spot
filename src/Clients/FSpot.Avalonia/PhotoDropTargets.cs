// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

public static class PhotoDropTargets
{
	public static IReadOnlyList<PhotoItemViewModel> ForTagDrop (
		PhotoItemViewModel? dropTarget,
		IEnumerable<PhotoItemViewModel> photos)
	{
		var items = photos?.ToList () ?? new List<PhotoItemViewModel> ();
		if (items.Count == 0)
			return Array.Empty<PhotoItemViewModel> ();

		if (dropTarget is null || dropTarget.Selected)
			return items.Where (photo => photo.Selected).ToList ();

		return new[] { dropTarget };
	}
}
