// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;

using FSpot.Avalonia.Services;
using FSpot.Core;

namespace FSpot.Avalonia.Printing;

public static class PrintWindowHost
{
	// The photos that have a file to print, with the details the labels can show.
	public static IReadOnlyList<PrintPhoto> Collect (IEnumerable<IPhoto> photos)
	{
		var list = new List<PrintPhoto> ();
		foreach (var photo in photos) {
			var paths = PhotoFileActions.SelectedPaths ([photo]);
			if (paths.Count > 0)
				list.Add (PrintPhoto.From (photo, paths[0]));
		}
		return list;
	}

	// Shows the print dialog; returns the page setup when the user changed it there.
	public static async Task<PrintPageSetup?> ShowAsync (Window owner, IReadOnlyList<PrintPhoto> photos, PrintPageSetup setup)
	{
		var window = new PrintWindow (photos, setup);
		await window.ShowDialog (owner);
		return ReferenceEquals (window.PageSetup, setup) ? null : window.PageSetup;
	}
}
